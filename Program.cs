new Game().Run();

//hej
public interface ITrainerFactory
{
    Trainer Create(string defaultName);
}

public abstract class TrainerFactory : ITrainerFactory
{
    public abstract Trainer Create(string defaultName);

    protected List<Pokemon> CreateTeam(int n)
    {
        List<Pokemon> team = new();
        for (int i = 0; i < n; i++)
            team.Add(CreateRandomPokemon());
        return team;
    }

    protected Pokemon CreateRandomPokemon()
    {
        // Normal attacks
        Attack tackle = new("Tackle", Element.Normal, 5, 0.9);
        Attack scratch = new("Scratch", Element.Normal, 5, 0.9);

        // Fire attacks
        Attack ember = new("Ember", Element.Fire, 10, 0.7);
        Attack fireSpin = new("Fire Spin", Element.Fire, 15, 0.5);

        // Water attacks
        Attack waterGun = new("Water Gun", Element.Water, 10, 0.7);
        Attack bubble = new("Bubble", Element.Water, 15, 0.5);

        // Grass attacks
        Attack razorLeaf = new("Razor Leaf", Element.Grass, 10, 0.7);
        Attack vineWhip = new("Vine Whip", Element.Grass, 15, 0.5);

        List<Pokemon> pokemon = [
            new("Bulbasaur", Element.Grass, [razorLeaf, vineWhip, tackle, scratch]),
            new("Oddish", Element.Grass, [razorLeaf, vineWhip, tackle, scratch]),
            new("Squirtle", Element.Water, [waterGun, bubble, tackle, scratch]),
            new("Psyduck", Element.Water, [waterGun, bubble, tackle, scratch]),
            new("Charmander", Element.Fire, [ember, fireSpin, tackle, scratch]),
            new("Vulpix", Element.Fire, [ember, fireSpin, tackle, scratch]),
        ];

        return pokemon[new Random().Next(pokemon.Count)];
    }
}

public class ComputerTrainerFactory
(
    IDisplay ui
) : TrainerFactory
{
    public override Trainer Create(string defaultName)
    {
        ui.PrintMessage($"Computer chose the name {defaultName}!");

        ui.PrintMessage($"What decision engine should {defaultName} use?");
        IDecisionEngine engine = new InteractiveNavigator<IDecisionEngine>(
            new VerticalMenu<IDecisionEngine>([
                new MenuItem<IDecisionEngine>("Random", new RandomDecisionEngine())
                // Student, add more strategies here!
            ]),
            ui
        ).Navigate();

        return new Trainer(
            defaultName,
            CreateRandomPokemon(),
            CreateTeam(2),
            engine
        );
    }
}

public class HumanTrainerFactory
(
    IDisplay ui
) : TrainerFactory
{
    public override Trainer Create(string defaultName)
    {
        ui.PrintMessage("What's your name?");
        string name = new InteractiveNavigator<string>(new AlphanumericMenu(10), ui).Navigate();
        if (name.Length > 0)
            ui.PrintMessage($"Welcome, {name}!");
        else
        {
            name = defaultName;
            ui.PrintMessage($"No name? Let's call you {name}.");
        }

        return new Trainer(
            name,
            CreateRandomPokemon(),
            CreateTeam(2),
            new InteractiveDecisionEngine()
        );
    }
}


public class Game
{
    public void Run()
    {
        Display ui = new();

        ui.PrintMessage("Welcome to the Arena!");

        ui.PrintMessage("What kind of game do you want to play?");
        ITrainerFactory humanFactory = new HumanTrainerFactory(ui);
        ITrainerFactory computerFactory = new ComputerTrainerFactory(ui);
        (ITrainerFactory, ITrainerFactory) factories =
            new InteractiveNavigator<(ITrainerFactory, ITrainerFactory)>(
                new VerticalMenu<(ITrainerFactory, ITrainerFactory)>([
                    new("Human vs Computer", (humanFactory, computerFactory)),
                    new("Human vs Human", (humanFactory, humanFactory)),
                    new("Computer vs Computer", (computerFactory, computerFactory)),
                ]),
                ui
            ).Navigate();

        ui.PrintMessage("Let's make player 1.");
        Trainer p1 = factories.Item1.Create("RED");
        ui.PrintMessage("Let's make player 2.");
        Trainer p2 = factories.Item2.Create("BLUE");

        Battle battle = new(p1, p2);
        battle.Run();
    }
}

public class Battle
(
    Trainer p1,
    Trainer p2
)
{
    IDisplay ui = new Display(new BattleComponent(p1, p2));
    Pokemon pokemon1 => p1.Active;
    Pokemon pokemon2 => p2.Active;
    bool noFaintedActivePokemon => !p1.Active.HasFainted && !p2.Active.HasFainted;

    public void Run()
    {
        ui.PrintMessage($"The battle begins.");

        while (p1.HasNonFaintedPokemon && p2.HasNonFaintedPokemon)
        {
            if (p1.Active.HasFainted)
            {
                ui.PrintMessage($"{p1.Name} must choose a new Pokemon.");
                p1.ChooseSwitchTarget(pokemon2, ui).Use(p1, p2.Active, ui);
                ui.PrintMessage($"{p2.Name}'s turn to choose an action.");
                p2.ChooseBattleAction(pokemon1, ui).Use(p2, p1.Active, ui);
            }
            else if (p2.Active.HasFainted)
            {
                ui.PrintMessage($"{p2.Name} must choose a new Pokemon.");
                p2.ChooseSwitchTarget(pokemon1, ui).Use(p2, p1.Active, ui);
                ui.PrintMessage($"{p1.Name}'s turn to choose an action.");
                p1.ChooseBattleAction(pokemon2, ui).Use(p1, p2.Active, ui);
            }
            else
            {
                ui.PrintMessage($"{p1.Name}'s turn to choose an action.");
                IAction p1Action = p1.ChooseBattleAction(pokemon2, ui);
                ui.PrintMessage($"{p2.Name}'s turn to choose an action.");
                IAction p2Action = p2.ChooseBattleAction(pokemon1, ui);

                if (new Random().NextDouble() > 0.5)
                {
                    p1Action.Use(p1, p2.Active, ui);
                    if (noFaintedActivePokemon)
                        p2Action.Use(p2, p1.Active, ui);
                }
                else
                {
                    p2Action.Use(p2, p1.Active, ui);
                    if (noFaintedActivePokemon)
                        p1Action.Use(p1, p2.Active, ui);
                }
            }
        }

        if (p1.HasNonFaintedPokemon)
            ui.PrintMessage($"{p1.Name} wins!");
        else if (p2.HasNonFaintedPokemon)
            ui.PrintMessage($"{p2.Name} wins!");
        else
            ui.PrintMessage("It's a draw!");
    }
}


public interface IDisplay
{
    void Redraw();
    void PrintMessage(string message, bool autoNext = false);
    void PrintMenu<T>(IMenu<T> menu);
}

public class Display : IDisplay
{
    IComponent component;
    string currentMessage = "";
    int textBoxWidth = 40;

    public Display() : this(new EmptyComponent()) { }
    public Display(IComponent component)
    {
        this.component = component;
    }

    public void Redraw()
    {
        Console.CursorVisible = false;
        Console.Clear();
        component.Draw();
        if (currentMessage.Length > 0)
            printMessageBox(currentMessage);
    }

    public void PrintMenu<T>(IMenu<T> menu)
    {
        Redraw();
        menu.Draw();
    }

    public void PrintMessage(string message, bool autoNext = false)
    {
        currentMessage = message;
        for (int i = 0; i < message.Length; i++)
        {
            if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Enter && !autoNext)
                break;
            currentMessage = message.Substring(0, i + 1);
            Redraw();
            Thread.Sleep(30);
        }
        int numLines = (int)Math.Ceiling(message.Length / (double)(textBoxWidth));
        int indexOfContinueChar = (textBoxWidth * numLines - 2 - message.Length);
        currentMessage = message + new string(' ', indexOfContinueChar) + " >";
        Redraw();
        if (!autoNext)
            Console.ReadKey();
        currentMessage = message;
        Redraw();
    }

    void printMessageBox(char c) => printMessageBox(c.ToString());
    void printMessageBox(string message)
    {
        int i = 0;
        Console.WriteLine(new string('–', textBoxWidth));
        while (i < message.Length)
        {
            int j = Math.Min(i + textBoxWidth, message.Length);
            Console.WriteLine(message.Substring(i, j - i));
            i = j;
        }
        Console.WriteLine(new string('–', textBoxWidth));
    }
}

public interface IComponent
{
    void Draw();
}

public class EmptyComponent : IComponent
{
    public void Draw() { }
}

public class BattleComponent
(
    Trainer p1,
    Trainer p2
) : IComponent
{
    Pokemon pokemon1 => p1.Active;
    Pokemon pokemon2 => p2.Active;

    public void Draw()
    {
        Console.WriteLine(p1.Summary);
        Console.WriteLine(pokemon1.Summary(pokemon2));
        Console.WriteLine(pokemon1.HealthBar);
        Console.WriteLine();
        Console.WriteLine(p2.Summary);
        Console.WriteLine(pokemon2.Summary(pokemon1));
        Console.WriteLine(pokemon2.HealthBar);
        Console.WriteLine();
    }
}


public class Trainer
(
    string name,
    Pokemon active,
    List<Pokemon> inactives,
    IDecisionEngine decisionEngine
)
{
    public Pokemon Active => active;
    public string Name => name;
    List<Pokemon> pokemons = [active, .. inactives];

    public bool HasActiveFainted => active.HasFainted;
    
    bool hasPokemonsToSwitchTo
    {
        get
        {
            foreach (Pokemon pokemon in pokemons)
                if (pokemon != active && !pokemon.HasFainted)
                    return true;
            return false;
        }
    }

    public bool HasNonFaintedPokemon
    {
        get
        {
            foreach (Pokemon pokemon in pokemons)
                if (!pokemon.HasFainted)
                    return true;
            return false;
        }
    }

    public bool HasStrongAgainst(Pokemon opponent)
    {
        foreach (Pokemon player in pokemons)
            if (player.IsStrongAgainst(opponent))
                return true;
        return false;
    }

    public string Summary
    {
        get
        {
            string summary = $"{Name} (";
            foreach (Pokemon pokemon in pokemons)
                summary += pokemon.HasFainted ? "x" : "*";
            summary += ")";
            return summary;
        }
    }

    public void SwitchTo(Pokemon target)
    {
        if (pokemons.Contains(target)) // Must be in team.
            active = target;
    }

    public Attack ChooseAttack(Pokemon opponent, IDisplay ui) =>
        decisionEngine.Pick(active.Attacks, this, opponent, ui);

    public Pokemon ChooseSwitchTarget(Pokemon opponent, IDisplay ui)
    {
        List<Pokemon> choices = new();
        foreach (Pokemon pokemon in pokemons)
            if (pokemon != active && !pokemon.HasFainted)
                choices.Add(pokemon);
        return decisionEngine.Pick(choices, this, opponent, ui);
    }

    public IAction ChooseBattleAction(Pokemon opponent, IDisplay ui)
    {
        List<BattleChoice> alternatives = hasPokemonsToSwitchTo
                ? [BattleChoice.Attack, BattleChoice.Switch]
                : [BattleChoice.Attack];
        BattleChoice choice = decisionEngine.Pick(alternatives, this, opponent, ui);

        switch (choice)
        {
            case BattleChoice.Attack:
                return ChooseAttack(opponent, ui);
            case BattleChoice.Switch:
                return ChooseSwitchTarget(opponent, ui);
            default:
                throw new Exception("Unknown choice!");
        }
    }
}

public interface IAction
{
    string Summary(Pokemon opponent);
    void Use(Trainer player, Pokemon opponent, IDisplay ui);
}

public enum BattleChoice
{
    Attack,
    Switch,
}

public interface IElemental
{
    bool IsStrongAgainst(Pokemon opponent);
    bool IsWeakAgainst(Pokemon opponent);
}

public class Pokemon
(
    string name,
    Element element,
    List<Attack> attacks,
    int health = 30,
    int maxHealth = 30
) : IElemental, IAction
{
    public string Name => name;
    public string Summary(Pokemon opponent) =>
        $"{Name} ({Health}) [{element}] {strengthSymbol(opponent)}";
    public List<Attack> Attacks => attacks;
    public int Health { get; private set; } = health;
    public int MaxHealth { get; } = maxHealth;
    public bool HasFainted => Health <= 0;

    public string HealthBar
    {
        get
        {
            int width = 38;
            int healthWidth = (int)Math.Ceiling((double)Health / MaxHealth * width);
            return '['
                + new string('~', healthWidth)
                + new string(' ', width - healthWidth)
                + ']';
        }
    }

    public bool IsStrongAgainst(Element opponent) =>
        element.IsStrongAgainst(opponent);

    public bool IsWeakAgainst(Element opponent) =>
        element.IsWeakAgainst(opponent);

    public bool IsStrongAgainst(Pokemon opponent) =>
        element.IsStrongAgainst(opponent);

    public bool IsWeakAgainst(Pokemon opponent) =>
        element.IsWeakAgainst(opponent);

    public void TakeDamage(int damage) =>
        Health = Math.Max(0, Health - damage);

    public void Use(Trainer attacker, Pokemon defender, IDisplay ui)
    {
        Pokemon old = attacker.Active;

        if (old == this || HasFainted)
            return; // Cannot switch to self or fainted Pokemon.

        if (!old.HasFainted)
            ui.PrintMessage($"{attacker.Name} called back {old.Name}.");

        attacker.SwitchTo(this);
        ui.PrintMessage($"{attacker.Name} chose {Name}. Go!");
    }
    
    string strengthSymbol(Pokemon opponent)
    {
        if (IsStrongAgainst(opponent))
            return "(+)";
        else if (IsWeakAgainst(opponent))
            return "(-)";
        else
            return "   ";
    }
}

public enum Element
{
    Normal,
    Fire,
    Water,
    Grass,
}

public static class ElementExtensions
{
    public static bool IsStrongAgainst(this Element attacker, Element defender) =>
        attacker switch
        {
            Element.Fire => defender == Element.Grass,
            Element.Water => defender == Element.Fire,
            Element.Grass => defender == Element.Water,
            _ => false
        };

    public static bool IsWeakAgainst(this Element attacker, Element defender) =>
        attacker switch
        {
            Element.Fire => defender == Element.Water,
            Element.Water => defender == Element.Grass,
            Element.Grass => defender == Element.Fire,
            _ => false
        };

    public static bool IsStrongAgainst(this Element attacker, Pokemon defender) =>
        defender.IsWeakAgainst(attacker);

    public static bool IsWeakAgainst(this Element attacker, Pokemon defender) =>
        defender.IsStrongAgainst(attacker);
}


public class Attack
(
    string name,
    Element element = Element.Normal,
    int damage = 10,
    double accuracy = 0.7
) : IElemental, IAction
{
    public string Name => name;

    public void Use(Trainer attacker, Pokemon defender, IDisplay ui)
    {
        if (attacker.HasActiveFainted || defender.HasFainted)
            return; // Cannot involve fainted Pokemon.

        ui.PrintMessage($"{attacker.Name}'s {attacker.Active.Name} used {Name}!");

        if (new Random().NextDouble() > accuracy)
        {
            ui.PrintMessage("It missed!");
            return;
        }

        int adjustedDamage = damage;
        if (element.IsStrongAgainst(defender))
            adjustedDamage *= 2;
        else if (element.IsWeakAgainst(defender))
            adjustedDamage /= 2;

        defender.TakeDamage(adjustedDamage);
        ui.Redraw();

        if (element.IsStrongAgainst(defender))
            ui.PrintMessage("It was super effective!");
        else if (element.IsWeakAgainst(defender))
            ui.PrintMessage("It was not very effective.");

        if (defender.HasFainted)
            ui.PrintMessage($"{defender.Name} fainted!");
    }

    public bool IsStrongAgainst(Pokemon enemy) =>
        enemy.IsWeakAgainst(element);

    public bool IsWeakAgainst(Pokemon enemy) =>
        enemy.IsStrongAgainst(element);

    public string Summary(Pokemon opponent)
    {
        string summary = $"{Name}".PadRight(15)
            + $"{damage} hp".PadRight(8)
            + $"{accuracy:P0}";
        if (element.IsStrongAgainst(opponent))
            return $"{summary} (+)";
        else if (element.IsWeakAgainst(opponent))
            return $"{summary} (-)";
        else
            return summary;
    }
}


public interface IDecisionEngine :
    IDecisionEngine<Attack>,
    IDecisionEngine<Pokemon>,
    IDecisionEngine<BattleChoice>
{ }

public interface IDecisionEngine<T>
{
    T Pick(List<T> alternatives, Trainer player, Pokemon opponent, IDisplay ui);
}

public class InteractiveDecisionEngine : IDecisionEngine
{
    public static T PickAction<T>(
        List<T> values, Trainer player, Pokemon opponent, IDisplay ui
    ) where T : IAction
    {
        List<MenuItem<T>> items = [];
        foreach (var value in values)
            items.Add(new MenuItem<T>(value.Summary(opponent), value));
        IMenu<T> menu = new VerticalMenu<T>(items);
        return new InteractiveNavigator<T>(menu, ui).Navigate();
    }

    public Attack Pick(List<Attack> attacks, Trainer player, Pokemon opponent, IDisplay ui)
        => PickAction<Attack>(attacks, player, opponent, ui);

    public Pokemon Pick(List<Pokemon> pokemons, Trainer player, Pokemon opponent, IDisplay ui)
        => PickAction<Pokemon>(pokemons, player, opponent, ui);

    public BattleChoice Pick(List<BattleChoice> choices, Trainer player, Pokemon opponent, IDisplay ui)
    {
        List<MenuItem<BattleChoice>> items = [];
        foreach (BattleChoice value in choices)
            items.Add(new MenuItem<BattleChoice>(value.ToString(), value));
        IMenu<BattleChoice> menu = new VerticalMenu<BattleChoice>(items);
        return new InteractiveNavigator<BattleChoice>(menu, ui).Navigate();
    }
}

public class RandomDecisionEngine : IDecisionEngine
{
    public static T Pick<T>(List<T> values, Trainer player, Pokemon opponent, IDisplay ui) =>
        values[new Random().Next(values.Count)];

    public Attack Pick(List<Attack> attacks, Trainer player, Pokemon opponent, IDisplay ui) =>
        Pick<Attack>(attacks, player, opponent, ui);

    public Pokemon Pick(List<Pokemon> pokemons, Trainer player, Pokemon opponent, IDisplay ui) =>
        Pick<Pokemon>(pokemons, player, opponent, ui);

    public BattleChoice Pick(List<BattleChoice> choices, Trainer player, Pokemon opponent, IDisplay ui) =>
        Pick<BattleChoice>(choices, player, opponent, ui);
}


public interface INavigator<T>
{
    T Navigate();
}

public class InteractiveNavigator<T>(IMenu<T> menu, IDisplay ui) : INavigator<T>
{
    public T Navigate()
    {
        while (true)
        {
            ui.PrintMenu(menu);
            var keyInfo = Console.ReadKey(true);
            switch (keyInfo.Key)
            {
                case ConsoleKey.Enter:
                    return menu.SelectedItem;
                case ConsoleKey.UpArrow:
                    menu.Up();
                    break;
                case ConsoleKey.DownArrow:
                    menu.Down();
                    break;
                case ConsoleKey.LeftArrow:
                    menu.Left();
                    break;
                case ConsoleKey.RightArrow:
                    menu.Right();
                    break;
                case ConsoleKey.Backspace:
                    menu.Delete();
                    break;
                default:
                    menu.SendChar(keyInfo.KeyChar);
                    break;
            }
        }
    }
}


public interface IMenu<T>
{
    void Draw();
    void Up();
    void Left();
    void Right();
    void Down();
    void SendChar(Char c);
    void Delete();
    T SelectedItem { get; }
}

public class MenuItem<T>(string name, T value)
{
    public string Name { get; } = name;
    public T Value { get; } = value;
}

public class VerticalMenu<T>(
    IEnumerable<MenuItem<T>> items,
    int selectedIndex = 0
) : IMenu<T>
{
    readonly IList<MenuItem<T>> items = items.ToList();

    public void Draw()
    {
        for (int i = 0; i < items.Count; i++)
        {
            Console.Write(i == selectedIndex ? "> " : "  ");
            Console.WriteLine(items[i].Name);
        }
    }

    public void Up()
        => selectedIndex = Math.Clamp(selectedIndex - 1, 0, items.Count - 1);

    public void Down()
        => selectedIndex = Math.Clamp(selectedIndex + 1, 0, items.Count - 1);

    public void Left() => Up();
    public void Right() => Down();
    public void SendChar(Char c) { }
    public void Delete() { }
    public T SelectedItem => items[selectedIndex].Value;
}

public class AlphanumericMenu(int maxLength) : IMenu<string>
{
    string value = "";
    
    public string SelectedItem => value;
    public void Up() { }
    public void Down() { }
    public void Left() { }
    public void Right() { }
    
    public void SendChar(Char c)
    {
        if (Char.IsLetterOrDigit(c) && value.Length < maxLength)
            value += Char.ToUpper(c);
    }

    public void Delete() =>
        value = value.Substring(0, Math.Max(0, value.Length - 1));

    public void Draw()
    {
        Console.WriteLine("Letters and digits only.");
        Console.Write("> " + value);
    }
}
