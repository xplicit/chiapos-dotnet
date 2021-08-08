using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using chiapos_dotnet;
using Chiapos.Dotnet.Disks;

namespace Chiapos.Dotnet
{
    public class DiskPlotter
    {
        // This method creates a plot on disk with the filename. Many temporary files
        // (filename + ".table1.tmp", filename + ".p2.t3.sort_bucket_4.tmp", etc.) are created
        // and their total size will be larger than the final plot file. Temp files are deleted at the
        // end of the process.
        public void CreatePlotDisk(
            string tmp_dirname,
            string tmp2_dirname,
            string final_dirname,
            string filename,
            byte k,
            byte[] memo,
            byte[] id,
            uint buf_megabytes_input = 0,
            uint num_buckets_input = 0,
            ulong stripe_size_input = 0,
            byte num_threads_input = 0,
            bool nobitfield = false,
            bool show_progress = false)
        {
            Console.WriteLine("Plotting started");
            // Increases the open file limit, we will open a lot of files.

            if (k < Constants.kMinPlotSize || k > Constants.kMaxPlotSize)
            {
                throw new ArgumentException($"Plot size k= {k} is invalid");
            }

            uint stripe_size, buf_megabytes, num_buckets;
            byte num_threads;
            if (stripe_size_input != 0)
            {
                stripe_size = (uint) stripe_size_input;
            }
            else
            {
                stripe_size = 65536;
            }

            if (num_threads_input != 0)
            {
                num_threads = num_threads_input;
            }
            else
            {
                num_threads = 2;
            }

            if (buf_megabytes_input != 0)
            {
                buf_megabytes = buf_megabytes_input;
            }
            else
            {
                buf_megabytes = 4608;
            }

            if (buf_megabytes < 10)
            {
                throw new InsufficientMemoryException("Please provide at least 10MiB of ram");
            }

            // Subtract some ram to account for dynamic allocation through the code
            ulong thread_memory = num_threads * (2 * (stripe_size + 5000)) *
                (ulong) EntrySizes.GetMaxEntrySize(k, 4, true) / (1024 * 1024);
            ulong sub_mbytes = 5UL + (ulong) Math.Min(buf_megabytes * 0.05, 50) + thread_memory;
            if (sub_mbytes > buf_megabytes)
            {
                throw new InsufficientMemoryException("Please provide more memory. At least {sub_mbytes}");
            }

            ulong memory_size = ((ulong) (buf_megabytes - sub_mbytes)) * 1024 * 1024;
            double max_table_size = 0;
            for (byte i = 1; i <= 7; i++)
            {
                double memory_i = 1.3 * ((ulong) 1 << k) * EntrySizes.GetMaxEntrySize(k, i, true);
                if (memory_i > max_table_size)
                    max_table_size = memory_i;
            }

            if (num_buckets_input != 0)
            {
                num_buckets = Util.RoundPow2(num_buckets_input);
            }
            else
            {
                num_buckets =
                    2 * Util.RoundPow2((uint)Math.Ceiling(max_table_size / (memory_size * Constants.kMemSortProportion)));
            }

            if (num_buckets < Constants.kMinBuckets)
            {
                if (num_buckets_input != 0)
                {
                    throw new ArgumentException($"Minimum buckets is {Constants.kMinBuckets}");
                }

                num_buckets = Constants.kMinBuckets;
            }
            else if (num_buckets > Constants.kMaxBuckets)
            {
                if (num_buckets_input != 0)
                {
                    throw new ArgumentException($"Maximum buckets is {Constants.kMaxBuckets}");
                }

                double required_mem =
                    (max_table_size / Constants.kMaxBuckets) / Constants.kMemSortProportion / (1024 * 1024) +
                    sub_mbytes;
                throw new InsufficientMemoryException(
                    $"Do not have enough memory. Need {required_mem} MiB");
            }

            uint log_num_buckets = (uint) Math.Log2(num_buckets);
            Debug.Assert(Math.Log2(num_buckets) == Math.Ceiling(Math.Log2(num_buckets)));

            if (max_table_size / num_buckets < stripe_size * 30)
            {
                throw new ArgumentException("Stripe size too large");
            }

            Console.WriteLine(
                $"{Environment.NewLine}Starting plotting progress into temporary dirs: {tmp_dirname} and {tmp2_dirname}");
            Console.WriteLine($"ID: {Convert.ToHexString(id)}");
            Console.WriteLine($"Plot size is: {k}");
            Console.WriteLine($"Buffer size is: {buf_megabytes} MiB");
            Console.WriteLine($"Using {num_buckets} buckets");
            Console.WriteLine($"Using {num_threads} threads of stripe size {stripe_size}");

            var tmp_1_filenames = new List<string>();

            // The table0 file will be used for sort on disk spare. tables 1-7 are stored in their own
            // file.
            tmp_1_filenames.Add(Path.Combine(tmp_dirname, $"{filename}.sort.tmp"));
            for (byte i = 1; i <= 7; i++)
            {
                tmp_1_filenames.Add(Path.Combine(tmp_dirname, $"{filename}.table{i}.tmp"));
            }

            string tmp_2_filename = Path.Combine(tmp2_dirname, $"{filename}.2.tmp");
            string final_2_filename = Path.Combine(final_dirname, $"{filename}.2.tmp");
            string final_filename = Path.Combine(final_dirname, filename);

            // Check if the paths exist
            if (!Directory.Exists(tmp_dirname))
            {
                throw new ArgumentException($"Temp directory {tmp_dirname} does not exist");
            }

            if (!Directory.Exists(tmp2_dirname))
            {
                throw new ArgumentException($"Temp2 directory {tmp2_dirname} does not exist");
            }

            if (!Directory.Exists(final_dirname))
            {
                throw new ArgumentException($"Final directory {final_dirname} does not exist");
            }

            foreach (var p in tmp_1_filenames)
            {
                File.Delete(p);
            }

            File.Delete(tmp_2_filename);
            File.Delete(final_filename);

            var phaseFlags = show_progress ? PhaseFlags.ShowProgress : PhaseFlags.None;

            {
                // Scope for FileDisk
                var tmp_1_disks = new List<FileDisk>();
                foreach (var fname in tmp_1_filenames)
                    tmp_1_disks.Add(new FileDisk(fname));

                var tmp2_disk = new FileDisk(tmp_2_filename);

                Debug.Assert(id.Length == Constants.kIdLen);

                Console.Write(
                    $"{Environment.NewLine}Starting phase 1/4: Forward Propagation into tmp files... {DateTime.Now}");

                var p1 = new PerformanceTimer();
                var all_phases = new PerformanceTimer();
                List<ulong> table_sizes = new Phase1().RunPhase1(
                    tmp_1_disks,
                    k,
                    id,
                    tmp_dirname,
                    filename,
                    memory_size,
                    num_buckets,
                    log_num_buckets,
                    stripe_size,
                    num_threads,
                    phaseFlags
                );
                p1.PrintElapsed("Time for phase 1 =");

                ulong finalsize = 0;

                if (nobitfield)
                {
                    /*
                    // Memory to be used for sorting and buffers
                    std::unique_ptr<uint8_t[]> memory(new uint8_t[memory_size + 7]);
    
                    std::cout << std::endl
                          << "Starting phase 2/4: Backpropagation without bitfield into tmp files... "
                          << Timer::GetNow();
    
                    Timer p2;
                    std::vector<uint64_t> backprop_table_sizes = b17RunPhase2(
                        memory.get(),
                        tmp_1_disks,
                        table_sizes,
                        k,
                        id,
                        tmp_dirname,
                        filename,
                        memory_size,
                        num_buckets,
                        log_num_buckets,
                        show_progress);
                    p2.PrintElapsed("Time for phase 2 =");
    
                    // Now we open a new file, where the final contents of the plot will be stored.
                    uint32_t header_size = WriteHeader(tmp2_disk, k, id, memo, memo_len);
    
                    std::cout << std::endl
                          << "Starting phase 3/4: Compression without bitfield from tmp files into " << tmp_2_filename
                          << " ... " << Timer::GetNow();
                    Timer p3;
                    b17Phase3Results res = b17RunPhase3(
                        memory.get(),
                        k,
                        tmp2_disk,
                        tmp_1_disks,
                        backprop_table_sizes,
                        id,
                        tmp_dirname,
                        filename,
                        header_size,
                        memory_size,
                        num_buckets,
                        log_num_buckets,
                        show_progress);
                    p3.PrintElapsed("Time for phase 3 =");
    
                    std::cout << std::endl
                          << "Starting phase 4/4: Write Checkpoint tables into " << tmp_2_filename
                          << " ... " << Timer::GetNow();
                    Timer p4;
                    b17RunPhase4(k, k + 1, tmp2_disk, res, show_progress, 16);
                    p4.PrintElapsed("Time for phase 4 =");
                    finalsize = res.final_table_begin_pointers[11];
                    */
                }
                else
                {
                    Console.WriteLine(
                        $"{Environment.NewLine} Starting phase 2/4: Backpropagation into tmp files... {DateTime.Now}");

                    var p2 = new PerformanceTimer();
                    Phase2Results res2 = new Phase2().RunPhase2(
                        tmp_1_disks,
                        table_sizes,
                        k,
                        id,
                        tmp_dirname,
                        filename,
                        memory_size,
                        num_buckets,
                        log_num_buckets,
                        phaseFlags);
                    p2.PrintElapsed("Time for phase 2 =");

                    // Now we open a new file, where the final contents of the plot will be stored.
                    uint header_size = WriteHeader(tmp2_disk, k, id, memo);

                    Console.WriteLine(
                        $"{Environment.NewLine}Starting phase 3/4: Compression from tmp files into {tmp_2_filename} ... {DateTime.Now}");
                    var p3 = new PerformanceTimer();
                    Phase3Results res = new Phase3().RunPhase3(
                        k,
                        tmp2_disk,
                        res2,
                        id,
                        tmp_dirname,
                        filename,
                        header_size,
                        memory_size,
                        num_buckets,
                        log_num_buckets,
                        phaseFlags);
                    p3.PrintElapsed("Time for phase 3 =");

                    Console.WriteLine(
                        $"Starting phase 4/4: Write Checkpoint tables into {tmp_2_filename} ... {DateTime.Now}");
                    var p4 = new PerformanceTimer();
                    new Phase4().RunPhase4(k, (byte) (k + 1), tmp2_disk, res, phaseFlags, 16);
                    p4.PrintElapsed("Time for phase 4 =");
                    finalsize = res.final_table_begin_pointers[11];
                }

                // The total number of bytes used for sort is saved to table_sizes[0]. All other
                // elements in table_sizes represent the total number of entries written by the end of
                // phase 1 (which should be the highest total working space time). Note that the max
                // sort on disk space does not happen at the exact same time as max table sizes, so this
                // estimate is conservative (high).
                ulong total_working_space = table_sizes[0];
                for (byte i = 1; i <= 7; i++)
                {
                    total_working_space += table_sizes[i] * (ulong) EntrySizes.GetMaxEntrySize(k, i, false);
                }

                Console.WriteLine(
                    $"Approximate working space used (without final file): {(double) total_working_space / (1024 * 1024 * 1024)} GiB");

                Console.WriteLine($"Final File size: {(double) finalsize / (1024 * 1024 * 1024)} GiB");
                all_phases.PrintElapsed("Total time =");
            }

            foreach (var p in tmp_1_filenames)
            {
                File.Delete(p);
            }

            bool bRenamed = false;
            var copy = new PerformanceTimer();
            while (!bRenamed)
            {
                try
                {
                    File.Move(tmp_2_filename, final_filename, true);
                    bRenamed = true;
                    Console.WriteLine($"Renamed final file from {tmp_2_filename} to {final_filename}");

                }
                catch (Exception e)
                {
                    Console.WriteLine(
                        $"Could not rename {tmp_2_filename} to {final_filename}. Error {e.Message}. Retrying in five minutes.");
                    Thread.Sleep(TimeSpan.FromMinutes(5));
                }
            }
        }

        // Writes the plot file header to a file
        private uint WriteHeader(
            FileDisk plot_Disk,
            byte k,
            byte[] id,
            byte[] memo)
        {
            // 19 bytes  - "Proof of Space Plot" (utf-8)
            // 32 bytes  - unique plot id
            // 1 byte    - k
            // 2 bytes   - format description length
            // x bytes   - format description
            // 2 bytes   - memo length
            // x bytes   - memo

            var header_text = System.Text.Encoding.ASCII.GetBytes("Proof of Space Plot");

            ulong write_pos = 0;
            plot_Disk.Write(write_pos, header_text);
            write_pos += (ulong) header_text.Length;
            plot_Disk.Write(write_pos, id);
            write_pos += Constants.kIdLen;

            var k_buffer = new byte[1] {k};
            plot_Disk.Write(write_pos, k_buffer);
            write_pos += 1;

            var description = System.Text.Encoding.ASCII.GetBytes(Constants.kFormatDescription);
            var size_buffer = new byte[2];
            Util.IntToTwoBytes(size_buffer, (ushort) description.Length);
            plot_Disk.Write(write_pos, size_buffer);
            write_pos += 2;
            plot_Disk.Write(write_pos, description);
            write_pos += (uint) description.Length;

            Util.IntToTwoBytes(size_buffer, (ushort) memo.Length);
            plot_Disk.Write(write_pos, size_buffer);
            write_pos += 2;
            plot_Disk.Write(write_pos, memo);
            write_pos += (uint) memo.Length;

            byte[] pointers = new byte[10 * 8];
            Array.Clear(pointers, 0, pointers.Length);
            plot_Disk.Write(write_pos, pointers);
            write_pos += 10 * 8;

            long bytes_written =
                header_text.Length + Constants.kIdLen + 1 + 2 + description.Length + 2 + memo.Length + 10 * 8;
            Console.WriteLine($"Wrote: {bytes_written}");
            return (uint) bytes_written;
        }
    }
}