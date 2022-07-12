using System;

namespace pwd;

public interface IView
{
    void WriteLine(
        string text);

    void Write(
        string text);

    bool Confirm(
        string question);

    void Location(
        IContext context);

    void Clear();
}

public sealed class View
    : IView
{
    public IContext? Context { get; private set; }

    public void WriteLine(
        string text)
    {
        Console.WriteLine(text);
    }

    public void Write(
        string text)
    {
        Console.Write(text);
    }

    public bool Confirm(
        string question)
    {
        Console.Write($"{question} (y/N) ");
        return Console.ReadLine()?.ToUpperInvariant() == "Y";
    }

    public void Clear()
    {
        Console.Clear();
    }

    public void Location(
        IContext context)
    {
        Context = context;
    }
}