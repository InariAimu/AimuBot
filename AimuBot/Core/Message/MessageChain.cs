using System.Collections;

using AimuBot.Core.Message.Model;

namespace AimuBot.Core.Message;

public class MessageChain : IEnumerable<BaseChain>
{
    internal List<BaseChain> Chains { get; }

    /// <summary>
    /// Count
    /// </summary>
    public int Count => this.Count();

    internal MessageChain()
        => Chains = new();

    internal MessageChain(params BaseChain[] chain)
        => Chains = new(chain.Where(i => i != null));

    internal MessageChain(string text)
        => Chains = new() { TextChain.Create(text) };

    public static implicit operator MessageChain(string value) => new(value);

    internal void Add(BaseChain chain)
        => Chains.Add(chain);

    internal void AddRange(IEnumerable<BaseChain> chains)
        => Chains.AddRange(chains);

    public IEnumerator<BaseChain> GetEnumerator()
        => Chains.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public List<BaseChain> this[Range r]
    {
        get
        {
            var (offset, length) = r.GetOffsetAndLength(Chains.Count);
            return Chains.GetRange(offset, length);
        }
    }

    public string ToCsCode()
        => Chains.Aggregate("", (current, element) => current + element.ToCsCode());

    public BaseChain this[int index]
        => Chains[index];

    public List<BaseChain> this[Type type]
        => Chains.Where(c => c.GetType() == type).ToList();

    public List<BaseChain> this[BaseChain.ChainMode mode]
        => Chains.Where(c => c.Mode == mode).ToList();

    public List<BaseChain> this[BaseChain.ChainType type]
        => Chains.Where(c => c.Type == type).ToList();
}
