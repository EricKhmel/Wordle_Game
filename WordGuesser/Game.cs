using SQLite;

namespace WordGuesser;

[Table("game")]
public class Game
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public int Id { get; set; }

    public Boolean WorL { get; set; }

    public string Word { get; set; }

    public int Tries { get; set; }

    [Ignore]
    public string ResultImage => WorL ? "win.png" : "loss.png";

    public override string ToString()
    {
        return string.Format("\nGame ID:         {0}\nWorL:              {1}\n{2}\n# of tries:         {3}", Id, WorL, Word, Tries
            + "\n---------------------------");
    
    }
}
