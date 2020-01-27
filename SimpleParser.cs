using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace FunctionalPatches.SimpleParser
{
    public class InvalidSyntaxException : Exception
    {
        public InvalidSyntaxException() { }
        public InvalidSyntaxException(string message) : base(message) { }
    }
    public class SimpleParser<T> where T : EventArgs
    {
        public LanguageTable Table { get; private set; }
        public SimpleParser()
        {
            Table = new LanguageTable();
        }

        public Func<T,bool> Parse(string exp)
        {
            // first, we have to strip white spaces.
            var space_pattern = new Regex(@"\s+", RegexOptions.Compiled);
            var slim_exp = space_pattern.Replace(exp, "");
            // then we tokenize the slim string.
            var tokens = new SimpleTokenizer(Table).Tokenize(slim_exp);
            // then we construct the parameter expression.
            var param = Expression.Parameter(typeof(T), "e");
            // then we construct the expression body.
            var queue = ShuntingYard(tokens);
            var expression = ConstructBody(queue, param);
            // then we construct the lambda.
            var lambda = Expression.Lambda<Func<T, bool>>(expression, param);
            // finally, we return the compiled delegate.
            return lambda.Compile();
        }

        // the shunting yard algorithm that transforms the expression into a queue that's useful.
        private Queue<string> ShuntingYard(IEnumerable<string> tokens)
        {
            var iter = tokens.GetEnumerator();
            var output = new Queue<string>();
            var ops = new Stack<string>();
            while (iter.MoveNext())
            {
                var current = iter.Current;
                // if we encountered an open parenthesis, we simply push it onto the ops stack.
                if (current == "(")
                {
                    ops.Push(current);
                }
                // if we encountered a close parenthesis, we pop every operator from the stack until we find an open parenthesis.
                else if (current == ")")
                {
                    while(ops.Peek() != "(")
                    {
                        output.Enqueue(ops.Pop());
                    }
                    // if pop and peek doesn't throw an exception, we have successfully found an open parenthesis before the stack empties. So we discard the open parenthesis.
                    ops.Pop();
                }
                // if we encountered the operators.
                else if (Table.Operators.ContainsKey(current))
                {
                    // while there is still an operator at the top of the ops stack.
                    // we pop the operator from the ops stack to the output queue only if:
                    // 1. the operator is not an open parenthesis.
                    // 2. the operator at the top of the stack has higher precedence.
                    while (ops.Count > 0 && ops.Peek() != "(" && Table.Operators[ops.Peek()].Precedence > Table.Operators[current].Precedence)
                    {
                       output.Enqueue(ops.Pop());
                    }
                    // after poping thing that matters, we push the current operator onto the stack.
                    ops.Push(current);
                }
                // finally, if we found a unit, we simply push it to the output queue.
                else
                {
                    output.Enqueue(current);
                }
            }
            // finally, we pop everything that's left in the stack to the output queue.
            while(ops.Count > 0)
            {
                output.Enqueue(ops.Pop());
            }
            return output;
        }
        private Expression ConstructBody(Queue<string> queue, ParameterExpression param)
        {
            var exps = new Stack<Expression>();
            while(queue.Count > 0)
            {
                var current = queue.Dequeue();
                // if we get an operator, we use the builder function stored in our language table to construct the expression, and push the expression back on top of the stack.
                if (Table.Operators.ContainsKey(current))
                {
                    var builder = Table.Operators[current].Builder;
                    var exp = builder(exps);
                    exps.Push(exp);
                }
                //if the current item is a unit.
                else
                {
                    // we parse the unit and push it to the expression stack.
                    exps.Push(ParseUnit(current, param));
                }
            }
            return exps.Pop();
        }

        private Expression ParseUnit(string token, ParameterExpression param)
        {
            var items = token.Split(':');
            // because I didn't remove empty entries, var is always going to be there.
            var var = items[0];
            if (string.IsNullOrEmpty(var)) throw new InvalidSyntaxException($"The token '{token}' has an empty member accessor!");
            // the new grammar allows infinite layers of property or field accession using the dot operator. so we need to process that.
            var accessors = var.Split('.');
            try
            {
                var member_accessor = ParseAccessors(accessors, param);
                var field = typeof(T).GetField(accessors[0]); // the first name in the accessors list is always going to be the field name of the EventArgs type, we need it for value conversion.
                if (items.Length > 1) // meaning that we can access the second item without error.
                {
                    var constant = items[1];
                    // we only parse if there's actually anything.
                    if (!string.IsNullOrEmpty(constant))
                    {
                        var converter = TypeDescriptor.GetConverter(field.FieldType);
                        var value = converter.ConvertFromString(constant);
                        var const_exp = Expression.Constant(value);
                        return Expression.Equal(member_accessor, const_exp); // after all the parsing, we can return early if we get a constant expression, saving one branching operation.
                    }
                }
                // if there is no constant expression to evaluate for, we just return the member access expression to get the value (this happens when some member is a boolean already).
                return member_accessor;
            }
            catch (InvalidSyntaxException)
            {
                throw new InvalidSyntaxException($"the token '{token}' has an invalid member accessor (check your dots)!");
            }
        }
        private Expression ParseAccessors(string[] accs, ParameterExpression param)
        {
            Expression prev_expression = param;
            Expression current_expression = null;
            for (int i = 0; i < accs.Length; i++)
            {
                var name = accs[i];
                if (string.IsNullOrEmpty(name)) throw new InvalidSyntaxException();
                current_expression = Expression.PropertyOrField(prev_expression, name);
                prev_expression = current_expression;
            }
            return current_expression;
        }
    }
}
