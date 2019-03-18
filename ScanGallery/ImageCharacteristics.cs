using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanGallery
{
    public abstract class ImageCharactericsBase : IEquatable<ImageCharactericsBase>
    {
        public void Increase() => this.Count++;
        public void Decrease() => this.Count--;
        public bool IsOriginal => this.Count == 0;
        public void Reset() => this.Count = 0;
        public void SetCount(int input) => this.Count = input;
        public int Count { get; private set; }

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

        protected double Base { get; set; }
        protected double Delta { get; set; }
    }

    public class Brightness : ImageCharactericsBase
    {
        public Brightness()
        {
            this.Base = 0.0;
            this.Delta = 4.0;
        }

        public override double Value { get { return this.Base + this.Delta * this.Count; } }
    }

    public class Contrast : ImageCharactericsBase
    {
        public Contrast()
        {
            this.Base = 1.0;
            this.Delta = 1.05;
        }

        public override double Value { get { return this.Base * Math.Pow(this.Delta, this.Count); } }
    }
}
