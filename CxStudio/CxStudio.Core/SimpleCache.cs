using System.Collections.Concurrent;

namespace CxStudio.Core;

public class SimpleCache<T>
    where T : struct
{
    private readonly struct Record
    {
        public string Key { get; init; }
        public T Value { get; init; }
        public uint Weight { get; init; }
        public DateTime Created { get; init; }
    }

    private readonly ConcurrentDictionary<string, Record> _records = [];

    public uint MaxWeight;
    private uint _totalWeight = 0;
    public uint TotalWeight => Interlocked.CompareExchange(ref _totalWeight, 0, 0);


    public SimpleCache(uint maxWeight = 100)
    {
        MaxWeight = maxWeight;
    }

    private void PurgeOldRecords()
    {
        if (TotalWeight <= MaxWeight) return;

        uint deltaWeight = TotalWeight - MaxWeight;
        uint countWeight = 0;
        HashSet<string> keys = [];
        foreach (var rec in _records.AsReadOnly().Values.OrderBy(x => x.Created))
        {
            if (countWeight > deltaWeight) break;
            countWeight += rec.Weight;
            keys.Add(rec.Key);
        }

        foreach (var k in keys)
        {
            bool removed = _records.TryRemove(k, out Record removedRec);
            if (removed)
                Interlocked.Exchange(ref _totalWeight, TotalWeight - removedRec.Weight);
        }
    }

    public T? Get(string key)
    {
        bool result = _records.TryGetValue(key, out Record record);
        return result ? record.Value : null;
    }

    public void Add(string key, T value, uint weight = 1)
    {

        Record r = new()
        {
            Key = key,
            Value = value,
            Weight = weight,
            Created = DateTime.Now
        };

        _records[r.Key] = r;
        Interlocked.Exchange(ref _totalWeight, TotalWeight + r.Weight);
        PurgeOldRecords();
    }

    public void Remove(string key)
    {
        bool res = _records.TryRemove(key, out Record removedRec);
        if (res)
            Interlocked.Exchange(ref _totalWeight, TotalWeight - removedRec.Weight);
    }

    public void Clear()
    {
        _records.Clear();
        Interlocked.Exchange(ref _totalWeight, 0);
    }


}
