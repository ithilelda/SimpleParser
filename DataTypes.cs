using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;

namespace FunctionalPatches.SimpleParser
{
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
        public readonly Func<Stack<StackNode>, StackNode> Builder;

        public Operator(string name, int precedence, bool isLeftAssociative, Func<Stack<StackNode>, StackNode> builder)
        {
            Name = name;
            Precedence = precedence;
            IsLeftAssociative = isLeftAssociative;
            Builder = builder;
        }
    }
    public class Function
    {
        public delegate StackNode FunctionBuilder(params Expression[] exp);
        public readonly string MethodName;
        public readonly int ParameterCount;
        public readonly FunctionBuilder Builder;
        public readonly Type[] ParameterTypes;

        public Function(string methodName, int parameterCount, FunctionBuilder builder, params Type[] parameterTypes)
        {
            MethodName = methodName;
            ParameterCount = parameterCount;
            Builder = builder;
            ParameterTypes = parameterTypes;
            if (ParameterTypes.Length != ParameterCount) throw new ArgumentException("Parameter count and Parameter types don't match!");
        }

        public StackNode BuildExpression(Stack<StackNode> exps)
        {
            var param = new Expression[ParameterCount];
            // because the stack pops backwards, we need to access the type backwards too.
            for (int i = ParameterCount - 1; i >= 0; i--)
            {
                var cur_stack = exps.Pop();
                var cur_exp = cur_stack.Exp;
                var cur_value = cur_stack.Raw;
                // if the current node on the expression stack has an expression, we simply add it to our parameter array.
                // we also need to check if the type is right.
                if (cur_exp != null)
                {
                    var type = Type.GetType(cur_value);
                    if (type != ParameterTypes[i]) throw new InvalidSyntaxException($"The type of your {i}th paramter is not {ParameterTypes[i]}!");
                    param[i] = cur_exp;
                }
                // otherwise, we need to convert and construct a constant expression.
                else if (!string.IsNullOrEmpty(cur_value))
                {
                    object value = null;
                    var cur_type = ParameterTypes[i];
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
            return Builder(param);
        }
    }
    public class InvalidSyntaxException : Exception
    {
        public InvalidSyntaxException() { }
        public InvalidSyntaxException(string message) : base(message) { }
    }
}
