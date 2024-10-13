namespace NaryMaps.Tests;

public class DocTests
{
    [Test]
    public void Test()
    {
        var map = NaryMap.New<MovieSchema>();

        var Lanthimos = new Director("Yorgos", "Lanthimos");
        var Noé = new Director("Gaspar", "Noé");
        var Koreeda = new Director("Hirokazu", "Kore-eda");
        
        map.AsSet().UnionWith(new (string Eidr, int ReleaseYear, FilmGenre Genre, Director Director, Movie Movie)[]
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
        
        var movieSet = map.AsSet();
        // ISet<(string Eidr, int ReleaseYear, FilmGenre Genre, Director Director, Movie Movie)>

        foreach (var movieTuple in movieSet) Console.WriteLine(movieTuple);
        /*
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
        */

        var m1 = ("10.5240/A9E0-6470-5F75-D926-2622-3", 2018, FilmGenre.Drama, Koreeda, new Movie("Manbiki Kazoku"));
        var m2 = ("10.5240/BC46-2751-9A86-763E-5848-7", 2019, FilmGenre.Drama, Noé, new Movie("Lux Æterna"));
        
        Console.WriteLine(movieSet.Contains(m1));
        // True

        Console.WriteLine(movieSet.Contains(m2));
        // False
        
        var movieSetByDirectors = map.With(s => s.Director).AsReadOnlyDictionaryOfEnumerable();
        // IReadOnlyDictionary<Director, IEnumerable<(string Eidr, int ReleaseYear, FilmGenre Genre, Director Director, Movie Movie)>>
        
        foreach (var (director, movieTuples) in movieSetByDirectors)
        {
            Console.WriteLine(director);
            foreach (var movieTuple in movieTuples) Console.WriteLine(movieTuple);
        }
        /*
            «Hirokazu Kore-eda»
            (10.5240/A9E0-6470-5F75-D926-2622-3, 2018, Drama, «Hirokazu Kore-eda», «Manbiki Kazoku»)
            (10.5240/1F6C-3084-485A-7DE6-83E2-P, 2009, SciFi, «Hirokazu Kore-eda», «Kūki Ningyō»)
            (10.5240/0FA3-5233-16EB-E2C9-FA15-Q, 2008, Drama, «Hirokazu Kore-eda», «Aruitemo aruitemo»)
            «Gaspar Noé»
            (10.5240/7754-3475-E2CE-5EE4-1012-F, 2018, Horror, «Gaspar Noé», «Climax»)
            (10.5240/46A3-52CD-9113-A0E8-5763-0, 2015, Erotic, «Gaspar Noé», «Love»)
            (10.5240/D255-6ACB-F7CE-6F98-2F0A-A, 2009, Fantasy, «Gaspar Noé», «Enter the void»)
            (10.5240/FCC9-10CF-EADA-1AF4-85FD-K, 2002, Horror, «Gaspar Noé», «Irréversible»)
            «Yorgos Lanthimos»
            (10.5240/2A9A-64B2-0DBB-25D0-F53F-X, 2023, Fantasy, «Yorgos Lanthimos», «Poor Things»)
            (10.5240/96E5-9C79-CB1D-4FD7-5C86-H, 2018, History, «Yorgos Lanthimos», «The Favourite»)
            (10.5240/C770-FE58-9161-3C59-FE74-8, 2015, Comedy, «Yorgos Lanthimos», «The Lobster»)
            (10.5240/83C7-4830-5647-6F63-34FE-U, 2009, Drama, «Yorgos Lanthimos», «Κυνόδοντας»)
         */
        
        var movieSetByEidr = map.With(s => s.Eidr).AsReadOnlyDictionary();
        // IReadOnlyDictionary<string, (string Eidr, int ReleaseYear, FilmGenre Genre, Director Director, Movie Movie)>
        
        Console.WriteLine(movieSetByEidr["10.5240/0FA3-5233-16EB-E2C9-FA15-Q"]);
        // (10.5240/0FA3-5233-16EB-E2C9-FA15-Q, 2008, Drama, «Hirokazu Kore-eda», «Aruitemo aruitemo»)
        
        Console.WriteLine(movieSetByEidr.ContainsKey("10.5240/12FE-AE0C-4F84-A1DD-2261-L"));
        // False
        
        foreach (var (eidr, movieTuple) in movieSetByEidr) Console.WriteLine($"{eidr} => {movieTuple}");
        
        var movieOnlySetByDirectors = map
            .With(s => s.Director)
            .AsReadOnlyMultiDictionary(t => t.Movie);
        // IReadOnlyMultiDictionary<Director, Movie>
        
        foreach (var pair in movieOnlySetByDirectors) Console.WriteLine(pair);

        /*
        IReadOnlyConflictingSet<(string Eidr, int ReleaseYear, FilmGenre Genre, Director Director, Movie Movie)> a = map.AsReadOnlySet();
        IReadOnlySet<string> b = map.With(s => s.Eidr).AsReadOnlySet();
        IReadOnlyConflictingDictionary<string, (string Eidr, int ReleaseYear, FilmGenre Genre, Director Director, Movie Movie)> c = map.With(s => s.Eidr).AsReadOnlyDictionary();
        IReadOnlyDictionary<string, Movie> d = map.With(s => s.Eidr).AsReadOnlyDictionary(t => t.Movie);
        IReadOnlyDictionary<string, IEnumerable<(string Eidr, int ReleaseYear, FilmGenre Genre, Director Director, Movie Movie)>> e = map.With(s => s.Eidr).AsReadOnlyDictionaryOfEnumerable();
        IReadOnlyDictionary<string, IEnumerable<Movie>> f = map.With(s => s.Eidr).AsReadOnlyDictionaryOfEnumerable(t => t.Movie);
        IReadOnlyConflictingMultiDictionary<string, (string Eidr, int ReleaseYear, FilmGenre Genre, Director Director, Movie Movie)> g = map.With(s => s.Eidr).AsReadOnlyMultiDictionary();
        IReadOnlyMultiDictionary<string, Movie> h = map.With(s => s.Eidr).AsReadOnlyMultiDictionary(t => t.Movie);

        IConflictingSet<(string Eidr, int ReleaseYear, FilmGenre Genre, Director Director, Movie Movie)> a2 = map.AsSet();
        IConflictingDictionary<string, (string Eidr, int ReleaseYear, FilmGenre Genre, Director Director, Movie Movie)> c2 = map.With(s => s.Eidr).AsDictionary();
        IRemoveOnlyDictionary<string, Movie> d2 = map.With(s => s.Eidr).AsDictionary(t => t.Movie);
        IRemoveOnlyDictionary<string, IEnumerable<(string Eidr, int ReleaseYear, FilmGenre Genre, Director Director, Movie Movie)>> e2 = map.With(s => s.Eidr).AsDictionaryOfEnumerable();
        IRemoveOnlyDictionary<string, IEnumerable<Movie>> f2 = map.With(s => s.Eidr).AsDictionaryOfEnumerable(t => t.Movie);
        IConflictingMultiDictionary<string, (string Eidr, int ReleaseYear, FilmGenre Genre, Director Director, Movie Movie)> g2 = map.With(s => s.Eidr).AsMultiDictionary();
        IRemoveOnlyMultiDictionary<string, Movie> h2 = map.With(s => s.Eidr).AsMultiDictionary(t => t.Movie);
        */
    }
}

public enum FilmGenre
{
    Fantasy,
    Drama,
    Comedy,
    SciFi,
    Erotic,
    History,
    Horror,
}

public record Director(string FirstName, string LastName)
{
    public override string ToString() => $"«{FirstName} {LastName}»";
}

public record Movie(string Title)
{
    public override string ToString() => $"«{Title}»";
}

public sealed class MovieSchema :
    Schema<(string Eidr, int ReleaseYear, FilmGenre Genre, Director Director, Movie Movie)>
{
    public UniqueSearchableParticipant<string> Eidr { get; }
    public Participant<Movie> Movie { get; }
    public SearchableParticipant<Director> Director { get; }
    public Participant<int> ReleaseYear { get; }
    public Participant<FilmGenre> Genre { get; }

    public Composite<(int, FilmGenre)> YearAndGenre { get; }
    
    public MovieSchema()
    {
        Eidr = DeclareUniqueSearchableParticipant<string>();
        Movie = DeclareParticipant<Movie>();
        Director = DeclareSearchableParticipant<Director>();
        ReleaseYear = DeclareParticipant<int>();
        Genre = DeclareParticipant<FilmGenre>();
        
        YearAndGenre = DeclareComposite(ReleaseYear, Genre);
        
        Sign = Conclude(Eidr, ReleaseYear, Genre, Director, Movie);
    }

    protected override Signature Sign { get; }
}
