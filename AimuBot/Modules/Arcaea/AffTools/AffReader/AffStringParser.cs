namespace AimuBot.Modules.Arcaea.AffTools.AffReader;

public class AffStringParser
{
    private int pos;
    private string str;

    public AffStringParser(string str)
    {
        this.str = str;
    }

    public void Skip(int length)
    {
        pos += length;
    }

    public float ReadFloat(string? terminator = null)
    {
        var end = terminator != null ? str.IndexOf(terminator, pos) : str.Length - 1;
        var value = float.Parse(str.Substring(pos, end - pos));
        pos += end - pos + 1;
        return value;
    }

    public int ReadInt(string? terminator = null)
    {
        var end = terminator != null ? str.IndexOf(terminator, pos) : str.Length - 1;
        var value = int.Parse(str.Substring(pos, end - pos));
        pos += end - pos + 1;
        return value;
    }

    public bool ReadBool(string? terminator = null)
    {
        var end = terminator != null ? str.IndexOf(terminator, pos) : str.Length - 1;
        var value = bool.Parse(str.Substring(pos, end - pos));
        pos += end - pos + 1;
        return value;
    }

    public string ReadString(string? terminator = null)
    {
        var end = terminator != null ? str.IndexOf(terminator, pos) : str.Length - 1;
        var value = str.Substring(pos, end - pos);
        pos += end - pos + 1;
        return value;
    }

    public string Current => str[pos].ToString();

    public string Peek(int count) => str.Substring(pos, count);
}