namespace Modelling_a_VM_Management_System;

internal static class Constants
{
    internal const int PageSize = 512;
    internal const int CountOfElements = PageSize / sizeof(int);
    internal const int BitmapSize = CountOfElements / 8;
    internal const int PageDataSize = CountOfElements - sizeof(int);
    internal const int PageBufferSize = 3;
    internal const int PageHeaderSize = 2;
    internal const ushort Signature = 0x4D56; // VM signature
}

internal static class Program
{
    private static void Main()
    {
        VirtualMemory? vm = null;
        while (true)
            try
            {
                Console.WriteLine("Enter file name:");
                var fileName = Console.ReadLine();
                Console.WriteLine("Enter array size:");
                var empty = string.Empty;
                {
                    var arraySize = int.Parse(Console.ReadLine() ?? empty);

                    vm = new VirtualMemory(fileName, arraySize);

                    Console.WriteLine("VirtualMemory created successfully.\n");

                    while (true)
                    {
                        Console.WriteLine("\nSelect an option:");
                        Console.WriteLine("1. Set element at index");
                        Console.WriteLine("2. Read element at index");
                        Console.WriteLine("3. Write to all elements");
                        Console.WriteLine("4. Exit");

                        var option = int.Parse(Console.ReadLine() ?? string.Empty);

                        switch (option)
                        {
                            case 1:
                                Console.WriteLine("Enter index:");
                                var setIndex = int.Parse(Console.ReadLine() ?? string.Empty);
                                Console.WriteLine("Enter element value:");
                                var setValue = int.Parse(Console.ReadLine() ?? string.Empty);
                                vm[setIndex] = setValue;
                                Console.WriteLine($"Element at index {setIndex} set to {setValue}.");
                                break;

                            case 2:
                                Console.WriteLine("Enter index:");
                                var readIndex = int.Parse(Console.ReadLine() ?? string.Empty);
                                var readValue = vm[readIndex];
                                Console.WriteLine($"Element at index {readIndex}: {readValue}.");
                                break;

                            case 3:
                                Console.WriteLine("Writing…");

                                for (var i = 0; i < arraySize; i++)
                                {
                                    vm[i] = i;
                                    if (i % 100_000 == 0 || (i % (arraySize / 10) == 0 && arraySize < 1_000_000))
                                        Console.Write("█");
                                }

                                Console.WriteLine("\nAll elements have been written to.");
                                break;

                            case 4:
                                Console.WriteLine("Exiting program.");
                                return;

                            default:
                                Console.WriteLine("Invalid option selected.");
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine("You have to get access one more time\n\n");
                vm?.Close();
            }
    }
}