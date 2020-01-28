using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;

namespace FunctionalPatches.SimpleParser
{
    public delegate StackNode StackNodeBuilder(Stack<StackNode> stack);
    public class StackNode
    {
        public string Raw;
        public Expression Exp;
    }
    public class Operator
    {
        public readonly string Name;
        public readonly int Precedence;
        public readonly bool IsLeftAssociative;
        public readonly StackNodeBuilder Builder;

        public Operator(string name, int precedence, bool isLeftAssociative, StackNodeBuilder builder)
        {
            Name = name;
            Precedence = precedence;
            IsLeftAssociative = isLeftAssociative;
            Builder = builder;
        }

        // the factory method for constructing StackNodeBuilder delegate for functions.
        public static StackNodeBuilder FunctionStackNodeBuilderGenerator(Func<Expression[], StackNode> func, int parcount, params Type[] partypes)
        {
            return exps =>
            {
                var param = new Expression[parcount];
                // because the stack pops backwards, we need to access the type backwards too.
                for (int i = parcount - 1; i >= 0; i--)
                {
                    var cur_stack = exps.Pop();
                    var cur_exp = cur_stack.Exp;
                    var cur_value = cur_stack.Raw;
                    // if the current node on the expression stack has an expression, we simply add it to our parameter array.
                    // we also need to check if the type is right.
                    if (cur_exp != null)
                    {
                        var type = Type.GetType(cur_value);
                        if (type != partypes[i]) throw new InvalidSyntaxException($"The type of your {i}th paramter is not {partypes[i]}!");
                        param[i] = cur_exp;
                    }
                    // otherwise, we need to convert and construct a constant expression.
                    else if (!string.IsNullOrEmpty(cur_value))
                    {
                        object value = null;
                        var cur_type = partypes[i];
                        if (cur_value != "null")
                        {
                            var converter = TypeDescriptor.GetConverter(cur_type);
                            value = converter.ConvertFromString(cur_value);
                        }
                        var const_exp = Expression.Constant(value, cur_type);
                        param[i] = const_exp;
                    }
                    else throw new InvalidSyntaxException("The current token is empty. Check your grammar!");
                }
                return func(param);
            };
        }
    }
    public class InvalidSyntaxException : Exception
    {
        public InvalidSyntaxException() { }
        public InvalidSyntaxException(string message) : base(message) { }
    }
}
