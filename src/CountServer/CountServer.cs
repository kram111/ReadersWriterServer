using System;
using System.Threading;

namespace CountServer
{    /// <summary>
    /// Статический сервер с приоритетом писателей
    /// Читатели параллельны, писатели последовательны
    /// </summary>
    public static class Server
    {
        private static int count = 0;
        private static readonly object mutex = new object();
        private static int readersActive = 0;
        private static int writersWaiting = 0;
        private static bool writerActive = false;
        
        /// <summary>
        /// Возвращает текущее значение счётчика
        /// </summary>
        /// <returns>Текущее значение count</returns>
        public static int GetCount()
        {
            lock (mutex)
            {
                while (writerActive || writersWaiting > 0)
                {
                    Monitor.Wait(mutex);
                }
                readersActive++;
            }
            
            int result = count;
            
            lock (mutex)
            {
                readersActive--;
                if (readersActive == 0 && writersWaiting > 0)
                {
                    Monitor.Pulse(mutex);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Добавляет значение к счётчику
        /// </summary>
        /// <param name="value">Значение для добавления</param>
        public static void AddToCount(int value)
        {
            lock (mutex)
            {
                writersWaiting++;
                while (writerActive || readersActive > 0)
                {
                    Monitor.Wait(mutex);
                }
                writersWaiting--;
                writerActive = true;
            }
            
            count += value;
            
            lock (mutex)
            {
                writerActive = false;
                if (writersWaiting > 0)
                {
                    Monitor.Pulse(mutex);
                }
                else
                {
                    Monitor.PulseAll(mutex);
                }
            }
        }
        
        /// <summary>
        /// Сбрасывает состояние сервера
        /// Обнуляет count и все счётчики синхронизации
        /// </summary>
        public static void Reset()
        {
            lock (mutex)
            {
                count = 0;
                readersActive = 0;
                writersWaiting = 0;
                writerActive = false;
                Monitor.PulseAll(mutex);
            }
        }
    }
}