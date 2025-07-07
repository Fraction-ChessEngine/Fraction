using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace fraction;

public class Transpostable : IDictionary<Position, Transposinfo> {
    private Dictionary<Position, Transposinfo> dict = new();

    public Transposinfo this[Position key] {
        get => dict[key];
        set => throw new NotSupportedException();
    }

    public ICollection<Position> Keys => this.dict.Keys;

    public ICollection<Transposinfo> Values => this.dict.Values;

    public int Count => ((ICollection<KeyValuePair<Position, Transposinfo>>)dict).Count;

    public bool IsReadOnly => ((ICollection<KeyValuePair<Position, Transposinfo>>)dict).IsReadOnly;


    public void Add(Position key, Transposinfo value) {
        this.dict.Add(key, value);
    }

    public void Add(KeyValuePair<Position, Transposinfo> item) {
        ((ICollection<KeyValuePair<Position, Transposinfo>>)dict).Add(item);
    }

    public void Clear() {
        ((ICollection<KeyValuePair<Position, Transposinfo>>)dict).Clear();
    }

    public bool Contains(KeyValuePair<Position, Transposinfo> item) {
        return ((ICollection<KeyValuePair<Position, Transposinfo>>)dict).Contains(item);
    }

    public bool ContainsKey(Position key) {
        return this.dict.ContainsKey(key);
    }

    public void CopyTo(KeyValuePair<Position, Transposinfo>[] array, int arrayIndex) {
        ((ICollection<KeyValuePair<Position, Transposinfo>>)dict).CopyTo(array, arrayIndex);
    }

    public IEnumerator<KeyValuePair<Position, Transposinfo>> GetEnumerator() {
        return this.dict.GetEnumerator();
    }

    public bool Remove(Position key) {
        return this.dict.Remove(key);
    }

    public bool Remove(KeyValuePair<Position, Transposinfo> item) {
        return ((ICollection<KeyValuePair<Position, Transposinfo>>)this.dict).Remove(item);
    }

    public bool TryGetValue(Position key, [MaybeNullWhen(false)] out Transposinfo value) {
        return this.dict.TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return this.dict.GetEnumerator();
    }
}
