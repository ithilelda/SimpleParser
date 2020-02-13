using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace SimpleParser
{
    public abstract class Parser
    {

    }
    public class SimpleParser<T> : Parser where T : EventArgs
    {
        public ParameterExpression Param { get; private set; }
        public LanguageTable Table { get; private set; }
        public SimpleTokenizer Tokenizer { get; private set; }
        public SimpleParser(LanguageTable table)
        {
            Param = Expression.Parameter(typeof(T), "e");
            Table = table;
            Tokenizer = new SimpleTokenizer(table);
        } 

        public Func<T,bool> Parse(string exp)
        {
            // first, we have to strip white spaces.
            var space_pattern = new Regex(@"\s+", RegexOptions.Compiled);
            var slim_exp = space_pattern.Replace(exp, "");
            // then we tokenize the slim string.
            var tokens = Tokenizer.Tokenize(slim_exp);
            // then we use shunting yard to convert infix notation to reverse polish notation.
            var rpn = ShuntingYard(tokens);
            // then we construct the expression body.
            var expression = ConstructBody(rpn);
            // then we construct the lambda.
            var lambda = Expression.Lambda<Func<T, bool>>(expression, Param);
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
                if (!string.IsNullOrEmpty(current))
                {
                    // if we encountered an open parenthesis, we simply push it onto the ops stack.
                    if (current == "(")
                    {
                        ops.Push(current);
                    }
                    // if we encountered a close parenthesis, we pop every operator from the stack until we find an open parenthesis.
                    else if (current == ")")
                    {
                        while (ops.Count > 0 && ops.Peek() != "(")
                        {
                            output.Enqueue(ops.Pop());
                        }
                        // if we cannot find an open parenthesis after all the stack empties, we throw the invalid syntax exception with a clear message.
                        if (ops.Count == 0) throw new InvalidSyntaxException("The parentheses don't match.");
                        // if pop and peek doesn't throw an exception, we have successfully found an open parenthesis before the stack empties. So we discard the open parenthesis.
                        ops.Pop();
                    }
                    // if we encountered the argument separate operator comma (,), we have to deal with it differently.
                    // we will have to pop every operator from the stack until we found an open parenthesis.
                    else if (current == ",")
                    {
                        // we basically do the same thing as the close parenthesis.
                        while (ops.Count > 0 && ops.Peek() != "(")
                        {
                            output.Enqueue(ops.Pop());
                        }
                        // if we cannot find an open parenthesis after all the stack empties, we throw the invalid syntax exception with a clear message.
                        if (ops.Count == 0) throw new InvalidSyntaxException("The parentheses don't match in the function call syntax.");
                        // however, we don't need to pop the open parenthesis at all, so we omit the line.
                    }
                    // if we encountered the operators.
                    else if (Table.CheckOperator(current))
                    {
                        // while there is still an operator at the top of the ops stack.
                        // we pop the operator from the ops stack to the output queue only if:
                        // 1. the operator at the top of the stack is not an open parenthesis. AND
                        // 2. the operator at the top of the stack has higher precedence. OR
                        // 3. this operator is left associative and the operator at the top of the stack has same precedence.
                        while (ops.Count > 0)
                        {
                            var top = ops.Peek();
                            if (top == "(") break;
                            var stack_op = Table.GetOperator(top);
                            var current_op = Table.GetOperator(current);
                            if ((current_op.IsLeftAssociative && stack_op.Precedence == current_op.Precedence) || stack_op.Precedence > current_op.Precedence)
                            {
                                output.Enqueue(ops.Pop());
                            }
                            else break;
                        }
                        // after poping thing that matters, we push the current operator onto the stack.
                        // If anything went wrong above, we'll just let it throw exceptions and the program will later handle it.
                        ops.Push(current);
                    }
                    // finally, if we found a unit, we simply push it to the output queue.
                    else
                    {
                        output.Enqueue(current);
                    }
                }
            }
            // finally, we pop everything that's left in the stack to the output queue.
            while(ops.Count > 0)
            {
                var op = ops.Pop();
                if (op == "(") throw new InvalidSyntaxException("The parentheses don't match.");
                output.Enqueue(op);
            }
            return output;
        }
        private Expression ConstructBody(Queue<string> input)
        {
            var exps = new Stack<object>();
            while(input.Count > 0)
            {
                var current = input.Dequeue();
                // if we get an operator, we use the builder function stored in our language table to construct the expression, and push the expression back on top of the stack.
                if (Table.CheckOperator(current))
                {
                    var builder = Table.GetOperator(current).Builder;
                    var exp = builder(exps);
                    exps.Push(exp);
                }
                // a token starting with a dollar sign means that it is the root accessor, we need to construct the root accessor expression from it, and push it to the stack.
                else if (current.StartsWith("$"))
                {
                    var name = current.Substring(1);
                    var exp = Expression.Field(Param, name);
                    exps.Push(exp);
                }
                //if the current item is a unit, we push it verbatim onto the stack as a raw string.
                else
                {
                    exps.Push(current);
                }
            }
            return exps.Pop() as Expression;
        }
    }
}
