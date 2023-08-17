using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Test    
{
    public class Pallet
    {
        private static int _lastId;

        private int _id;
        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                _lastId = Math.Max(value, _lastId);
            }
        }

        public float Width;
        public float Height;
        public float Depth;
        public float Weight => Boxes.Sum(box => box.Weight) + 30;

        public List<Box> Boxes { get; set; }

        public DateTime ExpirationDate => Boxes.Min(box => box.ExpirationDate ?? DateTime.MaxValue);

        public float Volume => Boxes.Sum(box => box.Volume) + Width * Height * Depth;

        public Pallet()
        {
            Id = ++_lastId;
        }
        
        // Метод группировки паллет по дате истечения срока годности
        public static IEnumerable<IGrouping<DateTime, Pallet>> GroupByExpiration(List<Pallet> pallets)
        {
            return pallets.GroupBy(pallet => pallet.ExpirationDate.Date);
        }

        // Метод сортировки паллет по возрастанию даты истечения срока годности
        public static List<Pallet> SortByExpirationAsc(List<Pallet> pallets) =>
            pallets.OrderBy(pallet => pallet.ExpirationDate).ToList();

        // Метод сортировки паллет по весу
        public static List<Pallet> SortByWeight(List<Pallet> pallets)
            => pallets.OrderBy(pallet => pallet.Weight).ToList();
    }

    public class Box
    {
        public float Width { get; set; }
        public float Height { get; set; }
        public float Depth { get; set; }

        private DateTime? _expirationDate;
        public DateTime? ExpirationDate 
        {             
            get => _expirationDate ?? ProductionDate?.Add(TimeSpan.FromDays(100));
            set => _expirationDate = value;
        }

        public DateTime? ProductionDate;

        public float Weight { get; set; }

        public float Volume => Width * Height * Depth;
    }

    internal class Program
    {
        // Метод для вывода информации о паллетах
        public static void Show(List<Pallet> pallets)
        {
            foreach (Pallet pallet in pallets)
            {
                Console.WriteLine($"Pallet {pallet.Id}: ");
                Console.WriteLine($"\t ExpirationDate: {pallet.ExpirationDate.ToShortDateString()}: ");
                Console.WriteLine($"\t Weight: {pallet.Weight}: ");
                Console.WriteLine($"\t Volume: {pallet.Volume}: ");
            }
        }

        // Метод для вывода информации о группах паллет по дате истечения срока годности
        public static void ShowGroups(IEnumerable<IGrouping<DateTime, Pallet>> palletGroups)
        {
            foreach (var palletGroup in palletGroups)
            {
                Console.WriteLine($"Group by expiration date ({palletGroup.Key.ToShortDateString()}):");
                Show(palletGroup.ToList());
            }
            
        }

        // Метод для записи паллет в JSON файл
        public static void Write(List<Pallet> pallets, string path)
        {
            var json = JsonSerializer.Serialize(pallets);

            FileStream fileStream;
            try
            {
                fileStream = new FileStream(path, FileMode.OpenOrCreate);
            }
            catch (FileNotFoundException)
            {
                Console.Write("File not found");
                return;
            }

            var writer = new StreamWriter(fileStream);
            writer.Write(json);
            writer.Close();
        }

        // Метод для чтения паллет из JSON файла
        public static List<Pallet> Read(string path) =>
            JsonSerializer.Deserialize<List<Pallet>>(File.ReadAllText(path));
        
        public static void Main()
        {
            const string filename = "Example.json";
            
            var pallets = new List<Pallet>
            {
                // ... (исходный список паллет)
            };
            
            // Записываем и читаем паллеты в/из JSON файл
            Write(pallets, filename);
            var receivedPallets = Read(filename);

            // Сортируем и группируем паллеты и выводим результаты
            var sortedPallets = Pallet.SortByWeight(receivedPallets);
            var groups = Pallet.GroupByExpiration(sortedPallets)
                .OrderBy(grouping => grouping.Key);
            
            ShowGroups(groups);
            
            Console.WriteLine(new String('=', 30));

            var topThreePalletsByExpiration = Pallet.SortByExpirationAsc(receivedPallets);
            Show(topThreePalletsByExpiration);
        }
    }
}
