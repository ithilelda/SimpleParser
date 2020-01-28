using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;

namespace FunctionalPatches.SimpleParser
{
    public class LanguageTable
    {
        private Dictionary<string, Operator> Operators;

        public LanguageTable()
        {
            Operators = new Dictionary<string, Operator>()
            {
                { ":", new Operator("accessor", 5, true, BuildAccessor)},
                { ">=", new Operator(">=", 3, true, ComparisonBuilderGenerator(Expression.GreaterThanOrEqual)) },
                { "<=", new Operator("<=", 3, true, ComparisonBuilderGenerator(Expression.LessThanOrEqual)) },
                { ">", new Operator(">", 3, true, ComparisonBuilderGenerator(Expression.GreaterThan)) },
                { "<", new Operator("<", 3, true, ComparisonBuilderGenerator(Expression.LessThan)) },
                { "==", new Operator("==", 3, true, ComparisonBuilderGenerator(Expression.Equal)) },
                { "!=", new Operator("!=", 3, true, ComparisonBuilderGenerator(Expression.NotEqual)) },
                { "!", new Operator("!", 2, true, exps => Expression.Not((Expression)exps.Pop())) },
                { "&", new Operator("&", 1, true, SimpleBinaryBuilderGenerator(Expression.AndAlso)) },
                { "|", new Operator("|", 1, true, SimpleBinaryBuilderGenerator(Expression.OrElse)) },
            };
        }

        // the public interfaces for operator accessions.
        public Operator GetOperator(string key) => Operators[key];
        public bool CheckOperator(string key) => Operators.ContainsKey(key);
        public void AddOperator(string key, Operator op) => Operators.Add(key, op);
        public IEnumerable<string> GetKeys() => Operators.Keys;


        // the factory methods for constructing the StackNodeBuilder delegate that's needed to construct expressions.
        public static Expression BuildAccessor(Stack<object> exps)
        {
            // an access operator always have two operands, so we just pop twice. If any exceptions are thrown (ie stack not enough), we just propagate up and not deal with it.
            var right_op = exps.Pop();
            var left_op = exps.Pop();
            // if the right operand is not a raw string or the left operand is not an expression, we got a problem.
            if (!(right_op is string)) throw new InvalidSyntaxException("The right operrand of the access operator must be a string indicating the field/property name!");
            if (!(left_op is Expression)) throw new InvalidSyntaxException("The left operand of the access operator must be an expression!");
            var parent_exp = left_op as Expression;
            var parent_type = parent_exp.Type;
            var member_name = right_op as string;
            var property = parent_type.GetProperty(member_name);
            var field = parent_type.GetField(member_name);
            if (property != null) // we prioritize the accession of properties.
            {
                return Expression.Property(parent_exp, member_name);
            }
            else if (field != null)
            {
                return Expression.Field(parent_exp, member_name);
            }
            else throw new InvalidSyntaxException($"the type {parent_type.AssemblyQualifiedName} does not contain either a property or a field named {member_name}!");
        }
        public static ExpressionBuilder SimpleBinaryBuilderGenerator(Func<Expression, Expression, Expression> builder)
        {
            return exps =>
            {
                var right_op = (Expression)exps.Pop();
                var left_op = (Expression)exps.Pop();
                return builder(left_op, right_op);
            };
        }
        public static ExpressionBuilder ComparisonBuilderGenerator(Func<Expression,Expression,BinaryExpression> builder)
        {
            return exps =>
            {
                var right_op = exps.Pop();
                var left_op = exps.Pop();
                if (!(left_op is Expression) && !(right_op is Expression)) throw new InvalidSyntaxException("comparison operator requires at least one expression!");
                Expression left_exp, right_exp;
                // if the lefthandside is a string literal, we need to construct a constant expression from the type info of the right expression.
                if (left_op is string)
                {
                    object value = null;
                    right_exp = right_op as Expression;
                    var constant = left_op as string;
                    var type = right_exp.Type;
                    if (constant != "null")
                    {
                        var converter = TypeDescriptor.GetConverter(type);
                        value = converter.ConvertFromString(constant);
                    }
                    left_exp = Expression.Constant(value, type);
                }
                // if the righthandside is a string literal, we need to construct a constant expression from the type info of the left expression.
                else if (right_op is string)
                {
                    object value = null;
                    left_exp = left_op as Expression;
                    var constant = right_op as string;
                    var type = left_exp.Type;
                    if (constant != "null")
                    {
                        var converter = TypeDescriptor.GetConverter(type);
                        value = converter.ConvertFromString(constant);
                    }
                    right_exp = Expression.Constant(value, type);

                }
                else if (left_op is Expression && right_op is Expression)// if both operands are expressions, we just make a new comparison expression from them.
                {
                    left_exp = left_op as Expression;
                    right_exp = right_op as Expression;
                }
                else throw new InvalidSyntaxException("The Evaluation Stack has types other than Expression or string, check your program!");
                return builder(left_exp, right_exp);
            };
        }
    }
}
