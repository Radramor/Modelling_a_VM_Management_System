namespace Modelling_a_VM_Management_System
{
    internal struct Page
    {
        public int AbsolutePageNumber;
        public byte Status;

        public long WriteTime;
        public byte[] Bitmap;
        public readonly int[] PageData;

        public Page(int absolutePageNumber)
        {
            AbsolutePageNumber = absolutePageNumber;
            Status = 0;
            WriteTime = 0;
            Bitmap = new byte[Constants.BitmapSize];

            PageData = new int[Constants.PageDataSize];
        }

        public void SetBitmap()
        {
            Bitmap = new byte[Constants.BitmapSize];
            for (var i = 0; i < Constants.BitmapSize; i++)
            {
                var pageDataRestrict = i * 8 + 8 - Constants.PageDataSize;
                pageDataRestrict = Math.Max(pageDataRestrict, 0);

                for (var j = 0; j < 8 - pageDataRestrict; j++)
                {
                    var bit = (byte)Math.Pow(2, 7 - j);
                    if (PageData[j + i * 8] != 0) Bitmap[i] |= bit;
                }
            }
        }
    }
}