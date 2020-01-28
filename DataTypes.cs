using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;

namespace FunctionalPatches.SimpleParser
{
    public delegate Expression ExpressionBuilder(Stack<object> stack);
    public class Operator
    {
        public readonly string Name;
        public readonly int Precedence;
        public readonly bool IsLeftAssociative;
        public readonly ExpressionBuilder Builder;

        public Operator(string name, int precedence, bool isLeftAssociative, ExpressionBuilder builder)
        {
            Name = name;
            Precedence = precedence;
            IsLeftAssociative = isLeftAssociative;
            Builder = builder;
        }

        // the factory method for constructing StackNodeBuilder delegate for functions.
        public static ExpressionBuilder FunctionStackNodeBuilderGenerator(Func<Expression[], Expression> func, int parcount, params Type[] partypes)
        {
            return exps =>
            {
                var param = new Expression[parcount];
                // because the stack pops backwards, we need to access the type backwards too.
                for (int i = parcount - 1; i >= 0; i--)
                {
                    var cur_stack = exps.Pop();
                    // if the current node on the expression stack has an expression, we simply add it to our parameter array.
                    // we also need to check if the type is right.
                    if (cur_stack is Expression)
                    {
                        var cur_exp = cur_stack as Expression;
                        var type = cur_exp.Type;
                        if (type != partypes[i]) throw new InvalidSyntaxException($"The type of your {i}th paramter is not {partypes[i]}!");
                        param[i] = cur_exp;
                    }
                    // otherwise, we need to convert and construct a constant expression.
                    else if (cur_stack is string)
                    {
                        var cur_value = cur_stack as string;
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
                    else throw new InvalidSyntaxException("The current token is not an expected type! you must pass in wrong stack!");
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
