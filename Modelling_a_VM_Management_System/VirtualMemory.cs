namespace Modelling_a_VM_Management_System;

public class VirtualMemory
{
    private readonly long _arraySize;
    private readonly long _pageNum;
    private readonly Page[] _pageBuffer;
    private readonly FileStream _fileStream = null!;

    public VirtualMemory(string? fileName, long arraySize)
    {
        _arraySize = arraySize;
        _pageNum = (_arraySize + Constants.PageDataSize - 1) / Constants.PageDataSize;
        AccessCounter = 0;
        _pageBuffer = new Page[Constants.PageBufferSize];
        for (var i = 0; i < Constants.PageBufferSize; i++) _pageBuffer[i] = new Page(i);

        if (!File.Exists(fileName))
        {
            if (fileName != null) _fileStream = new FileStream(fileName, FileMode.CreateNew);
            WriteSignatureToFile();
            WriteZerosToFile();
        }
        else
        {
            _fileStream = new FileStream(fileName, FileMode.Open);
            if (!IsFileValid()) throw new ArgumentException("Invalid file");
        }

        for (var i = 0; i < Constants.PageBufferSize; i++) LoadPageIntoBuffer(i, GetOldestPageNumber());
    }

    private void WriteSignatureToFile()
    {
        var SignatureBytes = BitConverter.GetBytes(Constants.Signature);
        _fileStream.Write(SignatureBytes, 0, 2);
    }

    private void WriteZerosToFile()
    {
        var zeros = new byte[Constants.PageSize - 2];
        _fileStream.Write(zeros, 0, zeros.Length);
        for (var i = 0; i < _pageNum; i++) WritePageToFile(new Page(i));
    }

    private bool IsFileValid()
    {
        var signatureBytes = new byte[2];
        _fileStream.Read(signatureBytes, 0, 2);
        var signature = BitConverter.ToUInt16(signatureBytes, 0);
        return signature == Constants.Signature;
    }

    private bool IsBitmapValid(int bufferIndex)
    {
        for (var i = 0; i < Constants.PageDataSize; i++)
        {
            var expectedBit = _pageBuffer[bufferIndex].PageData[i] != 0;
            var bitmapByteIndex = i / 8;
            var bitmapBitIndex = 7 - i % 8; // считаем биты слева направо
            var actualBit = (_pageBuffer[bufferIndex].Bitmap[bitmapByteIndex] & (1 << bitmapBitIndex)) != 0;
            if (expectedBit != actualBit)
                return false;
        }

        return true;
    }

    private void LoadPageIntoBuffer(int bufferIndex, int pageNumber)
    {
        if (pageNumber == -1) return;

        var fileOffset = Constants.PageHeaderSize + (long)pageNumber * Constants.PageSize;
        _fileStream.Seek(fileOffset, SeekOrigin.Begin);

        _fileStream.Read(_pageBuffer[bufferIndex].Bitmap, 0, Constants.BitmapSize);
        var pageDataBytes = new byte[Constants.PageDataSize * sizeof(int)];

        _fileStream.Read(pageDataBytes, 0, pageDataBytes.Length);
        Buffer.BlockCopy(pageDataBytes, 0, _pageBuffer[bufferIndex].PageData, 0, pageDataBytes.Length);

        if (!IsBitmapValid(bufferIndex))
            throw new InvalidDataException();

        _pageBuffer[bufferIndex].AbsolutePageNumber = pageNumber;
        _pageBuffer[bufferIndex].Status = 0;
        _pageBuffer[bufferIndex].WriteTime = 0;

        AccessCounter++;
    }

    private void SavePageFromBuffer(int bufferIndex)
    {
        if (_pageBuffer[bufferIndex].AbsolutePageNumber != -1)
        {
            WritePageToFile(_pageBuffer[bufferIndex]);
            _pageBuffer[bufferIndex].Status = 0;
            _pageBuffer[bufferIndex].WriteTime = 0;
        }
    }

    private int GetOldestPageNumber()
    {
        var oldestIndex = 0;
        var oldestTime = _pageBuffer[0].WriteTime;
        for (var i = 1; i < Constants.PageBufferSize; i++)
            if (_pageBuffer[i].AbsolutePageNumber != -1 && _pageBuffer[i].WriteTime < oldestTime)
            {
                oldestIndex = i;
                oldestTime = _pageBuffer[i].WriteTime;
            }

        return _pageBuffer[oldestIndex].AbsolutePageNumber;
    }

    private void WritePageToFile(Page page)
    {
        page.SetBitmap();
        var fileOffset = Constants.PageHeaderSize + (long)page.AbsolutePageNumber * Constants.PageSize;
        _fileStream.Seek(fileOffset, SeekOrigin.Begin);
        _fileStream.Write(page.Bitmap, 0, Constants.BitmapSize);
        var pageDataBytes = new byte[Constants.PageDataSize * sizeof(int)];
        Buffer.BlockCopy(page.PageData, 0, pageDataBytes, 0, pageDataBytes.Length);
        _fileStream.Write(pageDataBytes, 0, pageDataBytes.Length);


        AccessCounter++;
    }

    private int FindPageInBuffer(int pageNumber)
    {
        for (var i = 0; i < Constants.PageBufferSize; i++)
            if (_pageBuffer[i].AbsolutePageNumber == pageNumber)
                return i;

        return -1;
    }

    private int GetEmptyBufferIndex()
    {
        var emptyIndex = 0;
        for (var i = 0; i < Constants.PageBufferSize; i++)
        {
            if (_pageBuffer[i].AbsolutePageNumber == -1) return i;

            if (_pageBuffer[i].WriteTime < _pageBuffer[emptyIndex].WriteTime) emptyIndex = i;
        }

        return emptyIndex;
    }

    public int this[long index]
    {
        get
        {
            if (index < 0 || index >= _arraySize) throw new IndexOutOfRangeException();

            var pageNumber = (int)(index / Constants.PageDataSize);
            var bufferIndex = FindPageInBuffer(pageNumber);

            if (bufferIndex == -1)
            {
                bufferIndex = GetEmptyBufferIndex();
                LoadPageIntoBuffer(bufferIndex, pageNumber);
            }

            var offset = (int)(index % Constants.PageDataSize);
            _pageBuffer[bufferIndex].Status = 1;
            _pageBuffer[bufferIndex].WriteTime = DateTime.Now.Ticks;
            return _pageBuffer[bufferIndex].PageData[offset];
        }
        set
        {
            if (index < 0 || index >= _arraySize) throw new IndexOutOfRangeException();

            var pageNumber = (int)(index / Constants.PageDataSize);
            var bufferIndex = FindPageInBuffer(pageNumber);

            if (bufferIndex == -1)
            {
                bufferIndex = GetEmptyBufferIndex();
                LoadPageIntoBuffer(bufferIndex, pageNumber);
            }

            var offset = (int)(index % Constants.PageDataSize);
            _pageBuffer[bufferIndex].PageData[offset] = value;
            _pageBuffer[bufferIndex].Status = 1;
            _pageBuffer[bufferIndex].WriteTime = DateTime.Now.Ticks;
            SavePageFromBuffer(bufferIndex);
        }
    }

    public int AccessCounter { get; private set; }

    private void Flush()
    {
        for (var i = 0; i < Constants.PageBufferSize; i++) SavePageFromBuffer(i);

        _fileStream.Flush();
    }

    public void Close()
    {
        Flush();
        _fileStream.Close();
    }
}