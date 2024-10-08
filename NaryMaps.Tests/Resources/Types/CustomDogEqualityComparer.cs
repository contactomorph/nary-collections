using System.Collections;

namespace NaryMaps.Tests.Resources.Types;

public sealed class CustomDogEqualityComparer : IEqualityComparer<Dog>, IEnumerable<(Dog dog, uint hashCode)>
{
    private readonly Dictionary<Dog, uint> _dogToHashCode;
    
    public CustomDogEqualityComparer(params (Dog Dog, uint HashCode)[] dogsWithHashCodes)
    {
        _dogToHashCode = dogsWithHashCodes.ToDictionary(p => p.Dog, p => p.HashCode);
    }

    public CustomDogEqualityComparer(IEnumerable<(Dog Dog, uint HashCode)> dogsWithHashCodes)
    {
        _dogToHashCode = dogsWithHashCodes.ToDictionary(p => p.Dog, p => p.HashCode);
    }

    public bool Equals(Dog? x, Dog? y) => x?.Equals(y) ?? y is null;

    public int GetHashCode(Dog dog) => (int)_dogToHashCode[dog];
    public IEnumerator<(Dog dog, uint hashCode)> GetEnumerator()
    {
        return _dogToHashCode.Select(p => (p.Key, p.Value)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}