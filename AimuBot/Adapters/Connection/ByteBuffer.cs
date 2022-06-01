namespace AimuBot.Adapters.Connection;

public class ByteBuffer
{
    private byte[] _internalBuff;
    private int _readIndex = 0;
    private int _writeIndex = 0;
    private int _markReadIndex = 0;
    private int _markWirteIndex = 0;
    private int _capacity;

    private ByteBuffer(int capacity)
    {
        _internalBuff = new byte[capacity];
        _capacity = capacity;
    }

    private ByteBuffer(byte[] bytes)
    {
        _internalBuff = bytes;
        _capacity = bytes.Length;
    }

    public static ByteBuffer Allocate(int capacity)
        => new(capacity);

    public static ByteBuffer Allocate(byte[] bytes)
        => new(bytes);

    private byte[] flip(byte[] bytes)
    {
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    private int FixSizeAndReset(int currLen, int futureLen)
    {
        if (futureLen > currLen)
        {
            int size = FixLength(currLen) * 2;

            if (futureLen > size)
                size = FixLength(futureLen) * 2;

            byte[] newbuf = new byte[size];
            Array.Copy(_internalBuff, 0, newbuf, 0, currLen);
            _internalBuff = newbuf;
            _capacity = newbuf.Length;
        }
        return futureLen;
    }

    private int FixLength(int length)
    {
        int n = 2;
        int b = 2;
        while (b < length)
        {
            b = 2 << n;
            n++;
        }
        return b;
    }

    public ByteBuffer WriteBytes(byte[] bytes, int startIndex, int length)
    {
        lock (this)
        {
            int offset = length - startIndex;
            if (offset <= 0) return this;
            int total = offset + _writeIndex;
            int len = _internalBuff.Length;
            FixSizeAndReset(len, total);
            for (int i = _writeIndex, j = startIndex; i < total; i++, j++)
            {
                _internalBuff[i] = bytes[j];
            }
            _writeIndex = total;
        }
        return this;
    }

    public ByteBuffer WriteBytes(byte[] bytes, int length)
        => WriteBytes(bytes, 0, length);

    public ByteBuffer WriteBytes(byte[] bytes)
        => WriteBytes(bytes, bytes.Length);

    public ByteBuffer Write(ByteBuffer buffer)
    {
        if (buffer == null) return this;
        if (buffer.ReadableBytes() <= 0) return this;
        return WriteBytes(buffer.ToArray());
    }

    public ByteBuffer WriteShort(short value)
        => WriteBytes(flip(BitConverter.GetBytes(value)));

    public ByteBuffer WriteUshort(ushort value)
        => WriteBytes(flip(BitConverter.GetBytes(value)));

    public ByteBuffer WriteString(string value)
    {
        int len = value.Length;
        WriteInt(len);
        WriteBytes(System.Text.Encoding.UTF8.GetBytes(value));
        return this;
    }

    public string ReadString()
    {
        int len = ReadInt();
        byte[] bytes = new byte[len];
        ReadBytes(bytes, 0, len);

        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    public ByteBuffer WriteInt(int value) =>
        //byte[] array = new byte[4];
        //for (int i = 3; i >= 0; i--)
        //{
        //    array[i] = (byte)(value & 0xff);
        //    value = value >> 8;
        //}
        //Array.Reverse(array);
        //Write(array);
        WriteBytes(flip(BitConverter.GetBytes(value)));

    public ByteBuffer WriteUint(uint value) => WriteBytes(flip(BitConverter.GetBytes(value)));

    public ByteBuffer WriteLong(long value) => WriteBytes(flip(BitConverter.GetBytes(value)));

    public ByteBuffer WriteUlong(ulong value) => WriteBytes(flip(BitConverter.GetBytes(value)));

    public ByteBuffer WriteFloat(float value) => WriteBytes(flip(BitConverter.GetBytes(value)));

    public ByteBuffer WriteByte(byte value)
    {
        lock (this)
        {
            int afterLen = _writeIndex + 1;
            int len = _internalBuff.Length;
            FixSizeAndReset(len, afterLen);
            _internalBuff[_writeIndex] = value;
            _writeIndex = afterLen;
        }
        return this;
    }

    public ByteBuffer WriteDouble(double value) => WriteBytes(flip(BitConverter.GetBytes(value)));

    public byte ReadByte()
    {
        byte b = _internalBuff[_readIndex];
        _readIndex++;
        return b;
    }

    private byte[] Read(int len)
    {
        byte[] bytes = new byte[len];
        Array.Copy(_internalBuff, _readIndex, bytes, 0, len);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        _readIndex += len;
        return bytes;
    }

    public ushort ReadUshort() => BitConverter.ToUInt16(Read(2), 0);

    public short ReadShort() => BitConverter.ToInt16(Read(2), 0);

    public uint ReadUint() => BitConverter.ToUInt32(Read(4), 0);

    public int ReadInt() => BitConverter.ToInt32(Read(4), 0);

    public ulong ReadUlong() => BitConverter.ToUInt64(Read(8), 0);

    public long ReadLong() => BitConverter.ToInt64(Read(8), 0);

    public float ReadFloat() => BitConverter.ToSingle(Read(4), 0);

    public double ReadDouble() => BitConverter.ToDouble(Read(8), 0);

    public void ReadBytes(byte[] disbytes, int disstart, int len)
    {
        int size = disstart + len;
        for (int i = disstart; i < size; i++)
        {
            disbytes[i] = ReadByte();
        }
    }

    public void DiscardReadBytes()
    {
        if (_readIndex <= 0) return;
        int len = _internalBuff.Length - _readIndex;
        byte[] newbuf = new byte[len];
        Array.Copy(_internalBuff, _readIndex, newbuf, 0, len);
        _internalBuff = newbuf;
        _writeIndex -= _readIndex;
        _markReadIndex -= _readIndex;
        if (_markReadIndex < 0)
            _markReadIndex = _readIndex;
        _markWirteIndex -= _readIndex;
        if (_markWirteIndex < 0 || _markWirteIndex < _readIndex || _markWirteIndex < _markReadIndex)
            _markWirteIndex = _writeIndex;
        _readIndex = 0;
    }

    public void Clear()
    {
        _internalBuff = new byte[_internalBuff.Length];
        _readIndex = 0;
        _writeIndex = 0;
        _markReadIndex = 0;
        _markWirteIndex = 0;
    }

    public void SetReaderIndex(int index)
    {
        if (index < 0) return;
        _readIndex = index;
    }

    public int MarkReaderIndex()
    {
        _markReadIndex = _readIndex;
        return _markReadIndex;
    }

    public void MarkWriterIndex()
        => _markWirteIndex = _writeIndex;

    public void ResetReaderIndex()
        => _readIndex = _markReadIndex;

    public void ResetWriterIndex()
        => _writeIndex = _markWirteIndex;

    public int ReadableBytes()
        => _writeIndex - _readIndex;

    public byte[] ToArray()
    {
        byte[] bytes = new byte[_writeIndex];
        Array.Copy(_internalBuff, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    public int GetCapacity()
        => _capacity;
}

