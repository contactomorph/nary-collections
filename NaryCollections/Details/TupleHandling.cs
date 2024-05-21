namespace NaryCollections.Details;

public static class TupleHandling
{
    public static Type[] GetTupleTypeComposition(Type tupleType)
    {
        if (tupleType.IsConstructedGenericType)
        {
            return tupleType.GetGenericArguments();
        }

        throw new InvalidProgramException();
    }
    
    public static Type GetRepeatedTupleType<T>(int length)
    {
        switch (length)
        {
            case 0:
                return typeof(ValueTuple);
            case 1:
                return typeof(ValueTuple<T>);
            case 2:
                return typeof((T, T));
            case 3:
                return typeof((T, T, T));
            case 4:
                return typeof((T, T, T, T));
            case 5:
                return typeof((T, T, T, T, T));
            case 6:
                return typeof((T, T, T, T, T, T));
            default:
                throw new NotSupportedException();
        }
    }
}