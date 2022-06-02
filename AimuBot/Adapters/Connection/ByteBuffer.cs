using System.Text;

namespace AimuBot.Adapters.Connection;

public class ByteBuffer
{
    private int _capacity;
    private byte[] _internalBuff;
    private int _markReadIndex;
    private int _markWriteIndex;
    private int _readIndex;
    private int _writeIndex;

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
        if (futureLen <= currLen) return futureLen;
        var size = FixLength(currLen) * 2;

        if (futureLen > size)
            size = FixLength(futureLen) * 2;

        var newBuff = new byte[size];
        Array.Copy(_internalBuff, 0, newBuff, 0, currLen);
        _internalBuff = newBuff;
        _capacity = newBuff.Length;

        return futureLen;
    }

    private int FixLength(int length)
    {
        var n = 2;
        var b = 2;
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
            var offset = length - startIndex;
            if (offset <= 0) return this;
            var total = offset + _writeIndex;
            var len = _internalBuff.Length;
            FixSizeAndReset(len, total);
            for (int i = _writeIndex, j = startIndex; i < total; i++, j++) _internalBuff[i] = bytes[j];
            _writeIndex = total;
        }

        return this;
    }

    public ByteBuffer WriteBytes(byte[] bytes, int length)
        => WriteBytes(bytes, 0, length);

    public ByteBuffer WriteBytes(byte[] bytes)
        => WriteBytes(bytes, bytes.Length);

    public ByteBuffer Write(ByteBuffer buffer) => buffer.ReadableBytes() <= 0 ? this : WriteBytes(buffer.ToArray());

    public ByteBuffer WriteShort(short value)
        => WriteBytes(flip(BitConverter.GetBytes(value)));

    public ByteBuffer WriteUshort(ushort value)
        => WriteBytes(flip(BitConverter.GetBytes(value)));

    public ByteBuffer WriteString(string value)
    {
        var len = value.Length;
        WriteInt(len);
        WriteBytes(Encoding.UTF8.GetBytes(value));
        return this;
    }

    public string ReadString()
    {
        var len = ReadInt();
        var bytes = new byte[len];
        ReadBytes(bytes, 0, len);

        return Encoding.UTF8.GetString(bytes);
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
            var afterLen = _writeIndex + 1;
            var len = _internalBuff.Length;
            FixSizeAndReset(len, afterLen);
            _internalBuff[_writeIndex] = value;
            _writeIndex = afterLen;
        }

        return this;
    }

    public ByteBuffer WriteDouble(double value) => WriteBytes(flip(BitConverter.GetBytes(value)));

    public byte ReadByte()
    {
        var b = _internalBuff[_readIndex];
        _readIndex++;
        return b;
    }

    private byte[] Read(int len)
    {
        var bytes = new byte[len];
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
        var size = disstart + len;
        for (var i = disstart; i < size; i++) disbytes[i] = ReadByte();
    }

    public void DiscardReadBytes()
    {
        if (_readIndex <= 0) return;
        var len = _internalBuff.Length - _readIndex;
        var newBuff = new byte[len];
        Array.Copy(_internalBuff, _readIndex, newBuff, 0, len);
        _internalBuff = newBuff;
        _writeIndex -= _readIndex;
        _markReadIndex -= _readIndex;
        if (_markReadIndex < 0)
            _markReadIndex = _readIndex;
        _markWriteIndex -= _readIndex;
        if (_markWriteIndex < 0 || _markWriteIndex < _readIndex || _markWriteIndex < _markReadIndex)
            _markWriteIndex = _writeIndex;
        _readIndex = 0;
    }

    public void Clear()
    {
        _internalBuff = new byte[_internalBuff.Length];
        _readIndex = 0;
        _writeIndex = 0;
        _markReadIndex = 0;
        _markWriteIndex = 0;
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
        => _markWriteIndex = _writeIndex;

    public void ResetReaderIndex()
        => _readIndex = _markReadIndex;

    public void ResetWriterIndex()
        => _writeIndex = _markWriteIndex;

    public int ReadableBytes()
        => _writeIndex - _readIndex;

    public byte[] ToArray()
    {
        var bytes = new byte[_writeIndex];
        Array.Copy(_internalBuff, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    public int GetCapacity()
        => _capacity;
}