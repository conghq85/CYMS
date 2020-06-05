using System;
using System.Collections.Generic;
using System.Text;

namespace Tranbros.Sport
{
    public class OddDTO : BaseDTO
    {
        public string AtTime
        {
            get; set;
        }
        public string Score
        {
            get; set;
        }
        public string Odd
        {
            get;
            set;
        }
        public eOddType Type
        {
            get;
            set;
        }

        public float Home
        {
            get;
            set;
        }
        public float Away
        {
            get;
            set;
        }
        public float Draw
        {
            get;
            set;
        }
        public string Status
        {
            get; set;
        }
        public string OddTime
        {
            get; set;
        }

        public bool isRedHome
        {
            get; set;
        }
        public bool isRedAway
        {
            get; set;
        }
        public bool isRedOdd
        {
            get; set;
        }
        public bool isGreenHome
        {
            get; set;
        }
        public bool isGreenAway
        {
            get; set;
        }

        public float TongGia
        {
            get { return Home + Away; }
        }


    }
}
