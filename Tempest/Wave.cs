using System;
using System.IO;

namespace Tempest
{
    class Wave
    {
        private int bitDepth, sampleRate;
        private bool directionUp;
        short[] wave;
        double lastSin;

        public Wave()
        {
            this.sampleRate = 8000;
            this.bitDepth = 16;
            this.directionUp = true;
            this.wave = new short[0];
        }



        public void addWave(int freq, int len)
        {
            double sin;
            short[] data = new short[(int)Math.Floor((double)sampleRate * len / 1000)]; // Инициализируем массив 16 битных значений.
            double frequency = Math.PI * 2 * freq / this.sampleRate; // Рассчитываем требующуюся частоту.
            double shift = this.lastSin;
            if (this.directionUp)
            {
                shift = Math.Asin(shift);
            }
            else
            {
                shift = -Math.Asin(shift) + Math.PI;
            }
            int index;
            if (this.bitDepth == 8)
            {
                /*            for (int index = 0; index < this.sampleRate * len / 1000; index++) {
                                sin = Math.Sin(index * frequency + shift);
                                if (sin >= 0) {
                                    sin = floor(sin * maxint) + maxint + 1;
                                } else {
                                    $sin = floor($sin * ($maxint + 1) + ($maxint + 1));
                                }
                                $this->string .= self::encodeNumber($sin, $this->bitDepth / 8);
                            }*/
            }
            else
            {
                int maxint = (int)Math.Pow(2, this.bitDepth - 1) - 1;
                for (index = 0; index < this.sampleRate * len / 1000; index++)
                {
                    sin = Math.Sin(index * frequency + shift);
                    if (sin >= 0)
                    {
                        sin = Math.Floor(sin * maxint);
                    }
                    else
                    {
                        sin = Math.Floor(sin * (maxint + 1));
                    }
                    data[index] = (short)sin;
                    //$this->string .= self::encodeNumber($sin, $this->bitDepth / 8);
                }
            }
            index = (int)Math.Floor((double)this.sampleRate * len / 1000);
            this.lastSin = Math.Sin(index * frequency + shift);

            double phase = (len / 1000 + shift / (2 * Math.PI) / freq) * freq;
            phase -= (int)(phase);
            if (phase >= 0.25 && phase < 0.75)
            {
                this.directionUp = false;
            }
            else
            {
                this.directionUp = true;
            }

            var z = new short[wave.Length + data.Length];
            this.wave.CopyTo(z, 0);
            data.CopyTo(z, wave.Length);
            this.wave = z;
        }


        public void saveFile(Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            short frameSize = (short)(this.bitDepth / 8); // Количество байт в блоке (16 бит делим на 8).
            writer.Write(0x46464952); // Заголовок "RIFF".
            writer.Write(36 + this.wave.Length * frameSize); // Размер файла от данной точки.
            writer.Write(0x45564157); // Заголовок "WAVE".
            writer.Write(0x20746D66); // Заголовок "frm ".
            writer.Write(16); // Размер блока формата.
            writer.Write((short)1); // Формат 1 значит PCM.
            writer.Write((short)1); // Количество дорожек.
            writer.Write(this.sampleRate); // Частота дискретизации.
            writer.Write(this.sampleRate * frameSize); // Байтрейт (Как битрейт только в байтах).
            writer.Write(frameSize); // Количество байт в блоке.
            writer.Write((short)16); // разрядность.
            writer.Write(0x61746164); // Заголовок "DATA".
            writer.Write(this.wave.Length * frameSize); // Размер данных в байтах.
            for (int index = 0; index < this.wave.Length; index++)
            { // Начинаем записывать данные из нашего массива.
                foreach (byte element in BitConverter.GetBytes(wave[index]))
                { // Разбиваем каждый элемент нашего массива на байты.
                    stream.WriteByte(element); // И записываем их в поток.
                }
            }
        }

        public void addWave000(int freq, int len)
        {
            short[] data = new short[(int)Math.Floor((double)sampleRate * len / 1000) + 1]; // Инициализируем массив 16 битных значений.
            double frequency = Math.PI * 2 * freq / sampleRate; // Рассчитываем требующуюся частоту.

            double shift;
            shift = this.lastSin;
            if (this.directionUp)
            {
                shift = Math.Asin(shift);
            }
            else
            {
                shift = -Math.Asin(shift) + Math.PI;
            }

            for (int index = 0; index <= sampleRate * len / 1000; index++)
            { // Перебираем его.
                data[index] = (short)(Math.Sin(index * frequency + shift) * short.MaxValue); // Приводим уровень к амплитуде от 32767 до -32767.
            }

            int index1 = (int)Math.Floor((double)this.sampleRate * len / 1000) + 1;
            this.lastSin = Math.Sin(index1 * frequency + shift);
            if (Math.Sin((index1 + 0.000000000001) * frequency + shift) > this.lastSin)
            {
                this.directionUp = true;
            }
            else
            {
                this.directionUp = false;
            }

            var z = new short[wave.Length + data.Length];
            wave.CopyTo(z, 0);
            data.CopyTo(z, wave.Length);
            wave = z;
        }

        public static double Sine(int index, double frequency)
        {
            return Math.Sin(frequency * index);
        }

        private static double Saw(int index, double frequency)
        {
            return 2.0 * (index * frequency - Math.Floor(index * frequency)) - 1.0;
        }

        private static double Flat(int index, double frequency)
        {
            if (Math.Sin(frequency * index) > 0) return 1;
            else return -1;
        }

        private static double Triangle(int index, double frequency)
        {
            return 2.0 * Math.Abs(2.0 * (index * frequency - Math.Floor(index * frequency + 0.5))) - 1.0;
        }
    }
}