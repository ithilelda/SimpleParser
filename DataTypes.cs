using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace FunctionalPatches.SimpleParser
{
    public struct StackNode
    {
        public string Raw;
        public Expression Exp;
    }
    public class Operator
    {
        public readonly string Name;
        public readonly int Precedence;
        public readonly bool IsLeftAssociative;
        public readonly Func<Stack<StackNode>, StackNode> Builder;

        public Operator(string name, int precedence, bool isLeftAssociative, Func<Stack<StackNode>, StackNode> builder)
        {
            Name = name;
            Precedence = precedence;
            IsLeftAssociative = isLeftAssociative;
            Builder = builder;
        }
    }
    public class InvalidSyntaxException : Exception
    {
        public InvalidSyntaxException() { }
        public InvalidSyntaxException(string message) : base(message) { }
    }
}
