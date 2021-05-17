using System;

namespace TouchPortal.Plugin.AudioMonitor.Models
{
    public readonly struct Decibel : IComparable<Decibel>, IEquatable<Decibel>
    {
        public double Value { get; }

        public static Decibel Empty = new Decibel(double.NegativeInfinity);

        private Decibel(double value)
        {
            Value = value;
        }
        
        public static Decibel FromLinearPercentage(float linearPercentage)
        {
            if (linearPercentage > 1 || linearPercentage < 0)
                throw new ArgumentException("Must be a percentage between 0.0 and 1.0", nameof(linearPercentage));

            //Calculate decibels:
            var decibel = Math.Log10(linearPercentage) * 20;
            decibel = Math.Round(decibel);

            return new Decibel(decibel);
        }

        public static Decibel FromDecibelValue(double decibel)
        {
            return new Decibel(decibel);
        }

        #region Operators

        public int CompareTo(Decibel other)
            => Value.CompareTo(other.Value);

        public static bool operator > (Decibel left, Decibel right)
            => left.CompareTo(right) > 0;

        public static bool operator < (Decibel left, Decibel right)
            => left.CompareTo(right) < 0;

        public static bool operator >= (Decibel left, Decibel right)
            => left.CompareTo(right) >= 0;

        public static bool operator <= (Decibel left, Decibel right)
            => left.CompareTo(right) <= 0;

        public bool Equals(Decibel other)
            => Value.Equals(other.Value);

        public override bool Equals(object obj)
            => obj is Decibel other && Equals(other);

        public override int GetHashCode()
            => Value.GetHashCode();

        #endregion
    }
}
