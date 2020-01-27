using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;

namespace FunctionalPatches.SimpleParser
{
    public class LanguageTable<T> where T : EventArgs
    {
        public Dictionary<string, Operator> Operators { get; private set; }
        public ParameterExpression Param { get; private set; }

        public LanguageTable(ParameterExpression param)
        {
            Param = param;
            Operators = new Dictionary<string, Operator>()
            {
                { ":", new Operator(":", 5, true, BuildAccessor)},
                { ">=", new Operator(">=", 3, true, ComparisonBuilderGenerator(Expression.GreaterThanOrEqual)) },
                { "<=", new Operator("<=", 3, true, ComparisonBuilderGenerator(Expression.LessThanOrEqual)) },
                { ">", new Operator(">", 3, true, ComparisonBuilderGenerator(Expression.GreaterThan)) },
                { "<", new Operator("<", 3, true, ComparisonBuilderGenerator(Expression.LessThan)) },
                { "==", new Operator("==", 3, true, ComparisonBuilderGenerator(Expression.Equal)) },
                { "!=", new Operator("!=", 3, true, ComparisonBuilderGenerator(Expression.NotEqual)) },
                { "!", new Operator("!", 2, false, exps => new StackNode { Exp = Expression.Not(exps.Pop().Exp) }) },
                { "&", new Operator("&", 1, false, exps => new StackNode { Exp = Expression.And(exps.Pop().Exp, exps.Pop().Exp) }) },
                { "|", new Operator("|", 1, false, exps => new StackNode { Exp = Expression.Or(exps.Pop().Exp, exps.Pop().Exp) }) },
            };
        }
        private StackNode BuildAccessor(Stack<StackNode> exps)
        {
            // a colon operator always have two operands, so we just pop twice. If any exceptions are thrown (ie stack not enough), we just propagate up and not deal with it.
            var right_op = exps.Pop();
            var left_op = exps.Pop();
            // if the right operand is not a raw string, we got a problem.
            if (right_op.Exp != null || string.IsNullOrEmpty(right_op.Raw)) throw new InvalidSyntaxException("The right operrand of the access operator cannot be an expression!");
            if (left_op.Exp == null) throw new InvalidSyntaxException("The left operand of the access operator must be an expression!");
            Type type = Type.GetType(left_op.Raw); // the raw string of the left operand is where the assembly qualified type name is stored.
            string member_name = right_op.Raw;
            var property = type.GetProperty(member_name);
            var field = type.GetField(member_name);
            if (property != null) // we prioritize the accession of properties.
            {
                var full_name = property.PropertyType.AssemblyQualifiedName;
                return new StackNode { Raw = full_name, Exp = Expression.Property(left_op.Exp, member_name) };
            }
            else if (field != null)
            {
                var full_name = field.FieldType.AssemblyQualifiedName;
                return new StackNode { Raw = full_name, Exp = Expression.Field(left_op.Exp, member_name) };
            }
            else throw new InvalidSyntaxException($"the type {type.AssemblyQualifiedName} does not contain either a property or a field named {member_name}!");
        }
        private Func<Stack<StackNode>, StackNode> ComparisonBuilderGenerator(Func<Expression,Expression,BinaryExpression> builder)
        {
            return exps =>
            {
                var right_op = exps.Pop();
                var left_op = exps.Pop();
                if (left_op.Exp == null && right_op.Exp == null) throw new InvalidSyntaxException("comparison operator requires at least one expression!"); // one of the operands must be an expression.
                Expression comp_exp;
                // if the lefthandside is a string literal, we need to construct a constant expression from the type info of the right expression.
                if (left_op.Exp == null)
                {
                    var type = Type.GetType(right_op.Raw);
                    var converter = TypeDescriptor.GetConverter(type);
                    var value = converter.ConvertFromString(left_op.Raw);
                    var const_exp = Expression.Constant(value);
                    comp_exp = builder(const_exp, right_op.Exp);
                }
                // if the righthandside is a string literal, we need to construct a constant expression from the type info of the left expression.
                else if (right_op.Exp == null)
                {
                    var type = Type.GetType(left_op.Raw);
                    var converter = TypeDescriptor.GetConverter(type);
                    var value = converter.ConvertFromString(right_op.Raw);
                    var const_exp = Expression.Constant(value);
                    comp_exp = builder(left_op.Exp, const_exp);
                }
                else // if both operands are expressions, we just make a new comparison expression from them.
                {
                    comp_exp = builder(left_op.Exp, right_op.Exp);
                }
                return new StackNode { Exp = comp_exp };
            };
        }
    }
}
