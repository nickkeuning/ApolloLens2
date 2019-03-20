using System;

namespace ApolloLensLibrary.Imaging
{
    public abstract class ImageCharactericsBase : IEquatable<ImageCharactericsBase>
    {
        protected int Count { get; set; } = 0;

        public void Increase() => this.Count++;
        public void Decrease() => this.Count--;
        public bool IsDefault => this.Count == 0;
        public void Reset() => this.Count = 0;
        public void SetTo(ImageCharactericsBase other) => this.Count = other.Count;

        public bool Equals(ImageCharactericsBase other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (this.GetType() != other.GetType())
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return this.Count == other.Count;
        }

        public abstract double Value { get; }
    }

    public class Brightness : ImageCharactericsBase
    {
        private static double Base { get; } = 0.0;
        private static double Delta { get; } = 4.0;

        public override double Value { get { return Base + Delta * this.Count; } }
    }

    public class Contrast : ImageCharactericsBase
    {
        private static double Base { get; } = 1.0;
        private static double Delta { get; } = 1.05;

        public override double Value { get { return Base * Math.Pow(Delta, this.Count); } }
    }
}
