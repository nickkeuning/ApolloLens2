using System;

namespace ApolloLensLibrary.Imaging
{
    /// <summary>
    /// Specifies a discritized iterator like object
    /// which can describe (in this case) image brightness
    /// and contrast.
    /// </summary>
    public abstract class ImageCharactericsBase : IEquatable<ImageCharactericsBase>
    {
        protected int Count { get; set; } = 0;

        #region Operations

        public void Increase() => this.Count++;
        public void Decrease() => this.Count--;
        public bool IsDefault => this.Count == 0;
        public void Reset() => this.Count = 0;

        #endregion

        // note, should maybe be implemented in base classes,
        // since this would allow for setting a brightness
        // to a contrast.
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

        /// <summary>
        /// Implemented by base classes. Returns some value
        /// based on current count.
        /// </summary>
        public abstract double Value { get; }
    }

    /// <summary>
    /// Describes brightness, which varies additively
    /// </summary>
    public class Brightness : ImageCharactericsBase
    {
        private static double Base { get; } = 0.0;
        private static double Delta { get; } = 4.0;

        public override double Value { get { return Base + Delta * this.Count; } }
    }

    /// <summary>
    /// Describes contrast, which varies multiplicatively
    /// </summary>
    public class Contrast : ImageCharactericsBase
    {
        private static double Base { get; } = 1.0;
        private static double Delta { get; } = 1.05;

        public override double Value { get { return Base * Math.Pow(Delta, this.Count); } }
    }
}
