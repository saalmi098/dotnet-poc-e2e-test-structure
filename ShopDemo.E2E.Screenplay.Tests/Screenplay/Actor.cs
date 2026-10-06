namespace ShopDemo.E2E.Screenplay.Tests.Screenplay;

public sealed class Actor(string name)
{
    private readonly List<IAbility> _abilities = [];

    public string Name { get; } = name;

    public Actor Can(IAbility ability)
    {
        ArgumentNullException.ThrowIfNull(ability);
        _abilities.Add(ability);
        return this;
    }

    public T Ability<T>() where T : class, IAbility
        => _abilities.OfType<T>().SingleOrDefault()
            ?? throw new InvalidOperationException($"{Name} does not have the {typeof(T).Name} ability.");

    public async Task AttemptsToAsync(params ITask[] tasks)
    {
        foreach (var task in tasks)
        {
            await task.PerformAsAsync(this);
        }
    }

    public Task<T> AsksAsync<T>(IQuestion<T> question)
        => question.AnsweredByAsync(this);
}