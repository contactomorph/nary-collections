# Generalizing dictionaries, multidictionaries, bidictionaries, dictionaries with multiple keys, etc

Dictionaries (also known as maps or associative arrays), along with their related friends, sets, are ubiquitous data structures in programming. They are used to store key-value pairs and allow fast additions, removals and accesses by key. In C#, the `Dictionary<TKey, TValue>` class is the most common implementation of a dictionary. However, there are many variations of dictionaries that are not directly supported by the `Dictionary<TKey, TValue>` class. For example, a multidictionary (also known as multimap) is a dictionary that maps a key to a collection of values. A bidictionary is a dictionary that maps keys to values and values to keys. A dictionary with multiple keys is a dictionary that maps multiple types of keys to the same values. This library provides a generalization of dictionaries that can be used to implement all these variations and more.

## NaryMap as a set of tuples

A NaryMap always work as a set of tuples. To define one, you need to create a schema that defines the structure of the tuples. The schema is a class that inherits from the `Schema<TDataTuple>` class, where `TDataTuple` is the type of the tuple. The schema class must have a constructor that declares the components of the tuple (called `Participant`) and a property that returns the signature of the schema.

```csharp
public sealed class MovieSchema : Schema<(string Eidr, int ReleaseYear, FilmGenre Genre, Director Director, Movie Movie)>
{
    public Participant<string> Eidr { get; }
    public Participant<Movie> Movie { get; }
    public Participant<Director> Director { get; }
    public Participant<int> ReleaseYear { get; }
    public Participant<FilmGenre> Genre { get; }

    protected override Signature Sign { get; }
    
    public MovieSchema()
    {
        Eidr = DeclareParticipant<string>();
        Movie = DeclareParticipant<Movie>();
        Director = DeclareParticipant<Director>();
        ReleaseYear = DeclareParticipant<int>();
        Genre = DeclareParticipant<FilmGenre>();
        
        Sign = Conclude(Eidr, ReleaseYear, Genre, Director, Movie);
    }
}
```

Creating a new instance of NaryMap is done by calling the `New` method of the `NaryMap` class, passing the schema as a type parameter. It is empty at creation.

```csharp
var map = NaryMap.New<MovieSchema>();
```

You can use method `AsSet()` to convert the NaryMap to a set of tuples. The set is an instance of the `ISet<TDataTuple>` interface, where `TDataTuple` is the type of the tuple.

```csharp
var movieSet = map.AsSet();
// ISet<(string Eidr, int ReleaseYear, FilmGenre Genre, Director Director, Movie Movie)>

movieSet.UnionWith(new (string Eidr, int ReleaseYear, FilmGenre Genre, Director Director, Movie Movie)[]
{
    ("10.5240/FCC9-10CF-EADA-1AF4-85FD-K", 2002, FilmGenre.Horror, Noé, new Movie("Irréversible")),
    ("10.5240/0FA3-5233-16EB-E2C9-FA15-Q", 2008, FilmGenre.Drama, Koreeda, new Movie("Aruitemo aruitemo")),
    ("10.5240/83C7-4830-5647-6F63-34FE-U", 2009, FilmGenre.Drama, Lanthimos, new Movie("Κυνόδοντας")),
    ("10.5240/D255-6ACB-F7CE-6F98-2F0A-A", 2009, FilmGenre.Fantasy, Noé, new Movie("Enter the void")),
    ("10.5240/1F6C-3084-485A-7DE6-83E2-P", 2009, FilmGenre.SciFi, Koreeda, new Movie("Kūki Ningyō")),
    ("10.5240/C770-FE58-9161-3C59-FE74-8", 2015, FilmGenre.Comedy, Lanthimos, new Movie("The Lobster")),
    ("10.5240/46A3-52CD-9113-A0E8-5763-0", 2015, FilmGenre.Erotic, Noé, new Movie("Love")),
    ("10.5240/96E5-9C79-CB1D-4FD7-5C86-H", 2018, FilmGenre.History, Lanthimos, new Movie("The Favourite")),
    ("10.5240/A9E0-6470-5F75-D926-2622-3", 2018, FilmGenre.Drama, Koreeda, new Movie("Manbiki Kazoku")),
    ("10.5240/7754-3475-E2CE-5EE4-1012-F", 2018, FilmGenre.Horror, Noé, new Movie("Climax")),
    ("10.5240/2A9A-64B2-0DBB-25D0-F53F-X", 2023, FilmGenre.Fantasy, Lanthimos, new Movie("Poor Things")),
});
```

Enumerating the set displays the following output:
```
(10.5240/FCC9-10CF-EADA-1AF4-85FD-K, 2002, Horror, «Gaspar Noé», «Irréversible»)
(10.5240/0FA3-5233-16EB-E2C9-FA15-Q, 2008, Drama, «Hirokazu Kore-eda», «Aruitemo aruitemo»)
(10.5240/83C7-4830-5647-6F63-34FE-U, 2009, Drama, «Yorgos Lanthimos», «Κυνόδοντας»)
(10.5240/D255-6ACB-F7CE-6F98-2F0A-A, 2009, Fantasy, «Gaspar Noé», «Enter the void»)
(10.5240/1F6C-3084-485A-7DE6-83E2-P, 2009, SciFi, «Hirokazu Kore-eda», «Kūki Ningyō»)
(10.5240/C770-FE58-9161-3C59-FE74-8, 2015, Comedy, «Yorgos Lanthimos», «The Lobster»)
(10.5240/46A3-52CD-9113-A0E8-5763-0, 2015, Erotic, «Gaspar Noé», «Love»)
(10.5240/96E5-9C79-CB1D-4FD7-5C86-H, 2018, History, «Yorgos Lanthimos», «The Favourite»)
(10.5240/A9E0-6470-5F75-D926-2622-3, 2018, Drama, «Hirokazu Kore-eda», «Manbiki Kazoku»)
(10.5240/7754-3475-E2CE-5EE4-1012-F, 2018, Horror, «Gaspar Noé», «Climax»)
(10.5240/2A9A-64B2-0DBB-25D0-F53F-X, 2023, Fantasy, «Yorgos Lanthimos», «Poor Things»)
```

and you can use usual set methods:
```csharp
var m1 = ("10.5240/A9E0-6470-5F75-D926-2622-3", 2018, FilmGenre.Drama, Koreeda, new Movie("Manbiki Kazoku"));
var m2 = ("10.5240/BC46-2751-9A86-763E-5848-7", 2019, FilmGenre.Drama, Noé, new Movie("Lux Æterna"));

Console.WriteLine(movieSet.Contains(m1));
// True

Console.WriteLine(movieSet.Contains(m2));
// False
```

## NaryMap as a dictionary

Using a NaryMap as a set of tuple is convenient, but it does not really bring anything new compared to a `HashSet<TDataTuple>`. The real power of NaryMap comes from defining schemas where additional constraints are imposed on the tuples. For example, you can define alter the previous schema for movies by declaring the participant `string Eidr` to be unique. This way you look up a movie tuple by its [EIDR](https://ui.eidr.org/search) component. This is done by declaring the first component as a `UniqueSearchableParticipant<Eidr>` instead of a `Participant<Eidr>`. The `UniqueSearchableParticipant<T>` class is a subclass of `Participant<T>` that imposes the uniqueness constraint:

```csharp
public sealed class MovieSchema : Schema<(string Eidr, int ReleaseYear, FilmGenre Genre, Director Director, Movie Movie)>
{
    public UniqueSearchableParticipant<string> Eidr { get; }
    …
    public MovieSchema()
    {
        Eidr = DeclareUniqueSearchableParticipant<string>();
        …
    }
}
```

It is now possible to convert the NaryMap to a dictionary using the `With(participantSelector).AsReadOnlyDictionary()` method. The dictionary is an instance of the `IReadOnlyDictionary<TKey, TDataTuple>` interface.

```csharp
var movieDataByEidr = map.With(s => s.Eidr).AsReadOnlyDictionary();
// IReadOnlyDictionary<string, (string Eidr, int ReleaseYear, FilmGenre Genre, Director Director, Movie Movie)>
```

Enumerating this dictionary displays the following output:
```
10.5240/FCC9-10CF-EADA-1AF4-85FD-K → (10.5240/FCC9-10CF-EADA-1AF4-85FD-K, 2002, Horror, «Gaspar Noé», «Irréversible»)
10.5240/0FA3-5233-16EB-E2C9-FA15-Q → (10.5240/0FA3-5233-16EB-E2C9-FA15-Q, 2008, Drama, «Hirokazu Kore-eda», «Aruitemo aruitemo»)
10.5240/83C7-4830-5647-6F63-34FE-U → (10.5240/83C7-4830-5647-6F63-34FE-U, 2009, Drama, «Yorgos Lanthimos», «Κυνόδοντας»)
10.5240/D255-6ACB-F7CE-6F98-2F0A-A → (10.5240/D255-6ACB-F7CE-6F98-2F0A-A, 2009, Fantasy, «Gaspar Noé», «Enter the void»)
10.5240/1F6C-3084-485A-7DE6-83E2-P → (10.5240/1F6C-3084-485A-7DE6-83E2-P, 2009, SciFi, «Hirokazu Kore-eda», «Kūki Ningyō»)
10.5240/C770-FE58-9161-3C59-FE74-8 → (10.5240/C770-FE58-9161-3C59-FE74-8, 2015, Comedy, «Yorgos Lanthimos», «The Lobster»)
10.5240/46A3-52CD-9113-A0E8-5763-0 → (10.5240/46A3-52CD-9113-A0E8-5763-0, 2015, Erotic, «Gaspar Noé», «Love»)
10.5240/96E5-9C79-CB1D-4FD7-5C86-H → (10.5240/96E5-9C79-CB1D-4FD7-5C86-H, 2018, History, «Yorgos Lanthimos», «The Favourite»)
10.5240/A9E0-6470-5F75-D926-2622-3 → (10.5240/A9E0-6470-5F75-D926-2622-3, 2018, Drama, «Hirokazu Kore-eda», «Manbiki Kazoku»)
10.5240/7754-3475-E2CE-5EE4-1012-F → (10.5240/7754-3475-E2CE-5EE4-1012-F, 2018, Horror, «Gaspar Noé», «Climax»)
10.5240/2A9A-64B2-0DBB-25D0-F53F-X → (10.5240/2A9A-64B2-0DBB-25D0-F53F-X, 2023, Fantasy, «Yorgos Lanthimos», «Poor Things»)
```

and you can use usual dictionary methods:
```csharp
Console.WriteLine(movieDataByEidr["10.5240/A9E0-6470-5F75-D926-2622-3"]);
// (10.5240/A9E0-6470-5F75-D926-2622-3, 2018, Drama, «Hirokazu Kore-eda», «Manbiki Kazoku»)
Console.WriteLine(movieDataByEidr.ContainsKey("10.5240/BC46-2751-9A86-763E-5848-7"));
// False
```

You can also restrict the value type by providing a selector function to the `AsReadOnlyDictionary` method. For example, you can create a dictionary that maps EIDRs to movies:
```csharp
var movieByEidr = map.With(s => s.Eidr).AsReadOnlyDictionary(t => t.Movie);
// IReadOnlyDictionary<string, Movie>
```

Enumerating this dictionary displays the following output:
```
10.5240/FCC9-10CF-EADA-1AF4-85FD-K → «Irréversible»
10.5240/0FA3-5233-16EB-E2C9-FA15-Q → «Aruitemo aruitemo»
10.5240/83C7-4830-5647-6F63-34FE-U → «Κυνόδοντας»
10.5240/D255-6ACB-F7CE-6F98-2F0A-A → «Enter the void»
10.5240/1F6C-3084-485A-7DE6-83E2-P → «Kūki Ningyō»
10.5240/C770-FE58-9161-3C59-FE74-8 → «The Lobster»
10.5240/46A3-52CD-9113-A0E8-5763-0 → «Love»
10.5240/96E5-9C79-CB1D-4FD7-5C86-H → «The Favourite»
10.5240/A9E0-6470-5F75-D926-2622-3 → «Manbiki Kazoku»
10.5240/7754-3475-E2CE-5EE4-1012-F → «Climax»
10.5240/2A9A-64B2-0DBB-25D0-F53F-X → «Poor Things»
```

## NaryMap as a dictionary of enumerable values

Sometimes you want to look using a key that is not unique. For example, you may want to look up movies by their director. This is done by declaring the `Director` participant as a `SearchableParticipant<Director>` instead of a `Participant<Director>`. The `SearchableParticipant<T>` class is a subclass of `Participant<T>` that allows a participant to be used as a search key. However, contrary to `UniqueSearchableParticipant<T>`, multiple tuples can share the same key:

```csharp
public sealed class MovieSchema : Schema<(string Eidr, int ReleaseYear, FilmGenre Genre, Director Director, Movie Movie)>
{
    …
    public SearchableParticipant<Director> Director { get; }
    …
    public MovieSchema()
    {
        …
        Director = DeclareSearchableParticipant<Director>();
        …
    }
}
```

It is now possible to convert the NaryMap to a dictionary of enumerable values using the `With(participantSelector).AsReadOnlyDictionaryOfEnumerable()` method. The result is an instance of the `IReadOnlyDictionary<TKey, IEnumerable<TDataTuple>>` type.

```csharp
var movieDataByDirector = map.With(s => s.Director).AsReadOnlyDictionaryOfEnumerable();
// IReadOnlyDictionary<Director, IEnumerable<(string Eidr, int ReleaseYear, FilmGenre Genre, Director Director, Movie Movie)>>
```

As with dictionaries, you can restrict the value type by providing a selector function to the `AsReadOnlyDictionaryOfEnumerable` method. For example, you can create a dictionary that maps directors to release years:
```csharp
var releaseYearsByDirector = map.With(s => s.Director).AsReadOnlyDictionaryOfEnumerable(t => t.ReleaseYear);
// IReadOnlyDictionary<Director, IEnumerable<int>>
```

## NaryMap as a multidictionary

In NaryMap library, multidictionaries are represented by the interface `IReadOnlyMultiDictionary<TKey, TValue>`. This interface is similar to the `IReadOnlyDictionary<TKey, IEnumerable<TValue>>` interface, but it has a slightly different behaviour. For example the `Count` property returns the total number of pairs in the multidictionary, not the number of keys. A multidictionary is a collection of `KeyValuePair<TKey, TValue>` where keys can be repeated on enumeration as they may be associated with multiple values. Similarly to previous projection methods, you can use `AsReadOnlyMultiDictionary` to create a multidictionary. It comes in two flavors~: one with and one without a value selector~:

```csharp
var movieDataByDirector = map.With(s => s.Director).AsReadOnlyMultiDictionary();
// IReadOnlyMultiDictionary<Director, (string Eidr, int ReleaseYear, FilmGenre Genre, Director Director, Movie Movie)>
var releaseYearsByDirector = map.With(s => s.Director).AsReadOnlyMultiDictionary(t => t.ReleaseYear);
// IReadOnlyMultiDictionary<Director, int>
```

## Composites

Sometimes you do not want to access the data in the NaryMap using more than a single participant. For example, you may want to look up movies by combining their genre and release year. This is done by declaring a composite property `YearAndGenre` like in the following schema:

```csharp
public sealed class MovieSchema : Schema<(string Eidr, int ReleaseYear, FilmGenre Genre, Director Director, Movie Movie)>
{
    …
    public Composite<(int, FilmGenre)> YearAndGenre { get; }
    …
    public MovieSchema()
    {
        …
        YearAndGenre = DeclareComposite(ReleaseYear, Genre);
        …
    }
}
```

Composites are always searchable. This means you can use them to convert the NaryMap to a dictionary of enumerable by calling `With(compositeSelector).AsReadOnlyDictionaryOfEnumerable()` or to a multidictionary by calling `With(compositeSelector).AsReadOnlyMultiDictionary()`. Similarly to participants, you can also declare composites to be unique by using the `UniqueComposite<T>` class and `DeclareUniqueComposite<T1, …, Tn>()` methods. You then can convert the NaryMap to a dictionary using the `With(compositeSelector).AsReadOnlyDictionary()` method.