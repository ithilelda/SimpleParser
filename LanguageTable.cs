using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace FunctionalPatches.SimpleParser
{
    public class Operator
    {
        public readonly string Name;
        public readonly int Precedence;
        public readonly Func<Stack<Expression>, Expression> Builder;

        public Operator(string name, int precedence, Func<Stack<Expression>, Expression> builder)
        {
            Name = name;
            Precedence = precedence;
            Builder = builder;
        }
    }
    public class LanguageTable
    {
        public Dictionary<string, Operator> Operators { get; private set; }

        public LanguageTable()
        {
            Operators = new Dictionary<string, Operator>()
            {
                { "!", new Operator("!", 2, exps => Expression.Not(exps.Pop())) },
                { "&", new Operator("&", 2, exps => Expression.And(exps.Pop(), exps.Pop())) },
                { "|", new Operator("|", 2, exps => Expression.Or(exps.Pop(), exps.Pop())) },
            };
        }
    }
}
