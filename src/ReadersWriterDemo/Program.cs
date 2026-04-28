using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CountServer;

namespace ReadersWriter
{
    /// <summary>
    /// Демонстрация для статического сервера
    /// </summary>
    class Program
    {
        /// <summary>
        /// Точка входа в приложение
        /// Запуск с возможностью повторного запуска
        /// </summary>
        static void Main(string[] args)
        {
            bool exit = false;
            
            while (!exit)
            {
                Console.Clear();
                Console.WriteLine("Статический сервер с приоритетом писателей");
                
                // сброс состояния сервера перед новым запуском
                Server.Reset();
                
                Console.WriteLine("\nНастройки (можно нажать Enter для значений по умолчанию):");
                
                Console.Write("Количество читателей (по умолчанию 10): ");
                string readerInput = Console.ReadLine();
                int readerCount = string.IsNullOrEmpty(readerInput) ? 10 : int.Parse(readerInput);
                
                Console.Write("Количество писателей (по умолчанию 3): ");
                string writerInput = Console.ReadLine();
                int writerCount = string.IsNullOrEmpty(writerInput) ? 3 : int.Parse(writerInput);
                
                Console.Write("Операций на писателя (по умолчанию 5): ");
                string opsInput = Console.ReadLine();
                int operationsPerWriter = string.IsNullOrEmpty(opsInput) ? 5 : int.Parse(opsInput);
                
                Console.WriteLine($"\nЗапуск {readerCount} читателей и {writerCount} писателей...\n");
                
                var tasks = new List<Task>();
                var random = new Random();
                int totalReads = 0;
                int totalWrites = 0;
                var statsLock = new object();
                
                // запускаем читателей
                for (int i = 0; i < readerCount; i++)
                {
                    int readerId = i;
                    tasks.Add(Task.Run(() =>
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            int value = Server.GetCount();
                            lock (statsLock) { totalReads++; }
                            Console.WriteLine($"[READER {readerId:00}] Читают: {value}");
                            Thread.Sleep(random.Next(10, 100));
                        }
                    }));
                }
                
                // запускаем писателей
                for (int i = 0; i < writerCount; i++)
                {
                    int writerId = i;
                    tasks.Add(Task.Run(() =>
                    {
                        for (int j = 0; j < operationsPerWriter; j++)
                        {
                            int delta = random.Next(1, 10);
                            Console.WriteLine($"[WRITER {writerId:00}] Пытается добавить {delta}...");
                            var sw = Stopwatch.StartNew();
                            Server.AddToCount(delta);
                            sw.Stop();
                            lock (statsLock) { totalWrites++; }
                            Console.WriteLine($"[WRITER {writerId:00}] Добавлен {delta} (Взял {sw.ElapsedMilliseconds}ms)");
                            Thread.Sleep(random.Next(50, 200));
                        }
                    }));
                }
                
                // ждём завершения всех задач с таймаутом (защита от зависания)
                bool completed = Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(30));
                
                Console.WriteLine("\nРЕЗУЛЬТАТЫ");
                Console.WriteLine($"Всего чтений: {totalReads}");
                Console.WriteLine($"Всего записей: {totalWrites}");
                Console.WriteLine($"Финальное значение count: {Server.GetCount()}");
                
                if (!completed)
                {
                    Console.WriteLine("\nВнимание: Тест не завершился в течение 30 секунд!");
                }
                
                Console.WriteLine("\nНажмите:");
                Console.WriteLine("  Enter - запустить тест снова");
                Console.WriteLine("  Esc   - выйти из программы");
                
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape)
                {
                    exit = true;
                    Console.WriteLine("\nДо свидания!");
                }
            }
        }
    }
}