import std.sumtype;
import std.stdio;
import std.range;
import std.algorithm;
import std.math;

struct SymbolTable
{
    // must be sorted
    string[] names;
    float*[] addresses;

    Symbol find(string name) const
    {
        auto sorted = assumeSorted(names[]);
        auto t = sorted.trisect(name);
        if (t[1].length == 0)
            return Symbol(-1);
        return Symbol(t[0].length);
    }

    ref float get(Symbol symbol)
    {
        assert(symbol.index >= 0);
        return *addresses[symbol.index];
    }
}

SymbolTable createSymbolTableFromVariables(variables...)()
{
    import std.conv : to;

    SymbolTable table;
    static foreach (variable; variables)
    {{
        static if (is(typeof(variable) == float[]))
        {
            foreach (int index, ref value; variable)
            {
                table.names ~= variable.stringof ~ index.to!string;
                table.addresses ~= &value;
            }
        }
        else static if (is(typeof(variable) == float[N], size_t N))
        {
            foreach (int index, ref value; variable)
            {
                table.names ~= variable.stringof ~ index.to!string;
                table.addresses ~= &value;
            }
        }
        else static if (is(typeof(variable) == float))
        {
            table.names ~= variable.stringof;
            table.addresses ~= &variable;
        }
        else static if (is(typeof(variable) == float*))
        {
            table.names ~= variable.stringof;
            table.addresses ~= variable;
        }
        else static assert(0, typeof(variable));
    }}
    
    import std.range;
    import std.algorithm;

    int[] sortedIndices = iota(0, cast(int) table.names.length).array;
    sortedIndices[].sort!((a, b) => table.names[a] < table.names[b]);
    table.names = sortedIndices.map!(a => table.names[a]).array;
    table.addresses = sortedIndices.map!(a => table.addresses[a]).array;
    return table;
}

struct Symbol
{
    int index;
}

struct Expression
{
    // sum of product of terms
    Term[][] terms;
}

alias Inner = SumType!(Expression, Symbol, float);

struct Term
{
    Inner power;
    Inner inner;
}

Term term(Expression exp)
{
    if (exp.terms.length == 1 && exp.terms[0].length == 1)
        return exp.terms[0][0];
    if (exp.terms.length == 0)
        return Term(Inner(1.0f), Inner(0.0f));
    return Term(Inner(1.0f), Inner(exp));
}

Term term(Inner inner)
{
    return Term(Inner(1.0f), inner);
}

import std.array;
import std.format;

void print(TAppender)(ref TAppender appender, in SymbolTable symbolTable, const(Expression) expression)
{
    if (expression.terms.length == 0)
    {
        appender ~= "0";
        return;
    }

    foreach (termProductIndex, termProduct; expression.terms)
    {
        if (termProductIndex > 0)
            appender ~= " + ";

        foreach (termIndex, const ref term; termProduct)
        {
            if (termIndex > 0)
                appender ~= " * ";

            import std.stdio;

            void matchInner()
            {
                term.inner.match!(
                    (const Expression expression0)
                    {
                        appender ~= "(";
                        print(appender, symbolTable, expression0),
                        appender ~= ")";
                    },
                    (const Symbol symbol)
                    {
                        appender ~= symbolTable.names[symbol.index];
                    },
                    (const float constant)
                    {
                        appender.formattedWrite!"%g"(constant);
                    }
                );
            }

            term.power.match!(
                (const Expression expression0)
                {
                    matchInner();
                    appender ~= " ^ (";
                    print(appender, symbolTable, expression0);
                    appender ~= ")";
                },
                (const Symbol symbol)
                {
                    matchInner();
                    appender ~= " ^ ";
                    appender ~= symbolTable.names[symbol.index];
                },
                (const float constant)
                {
                    if (constant == 1)
                    {
                        matchInner();
                    }   
                    else if (constant == -1)
                    {
                        appender ~= "1 / ";
                        matchInner();
                    }
                    else
                    {
                        matchInner();
                        appender.formattedWrite!" ^ %g"(constant); 
                    }
                }
            );
        }
    }
}


import std.array;

enum ParseResultType
{
    ok,
    notMatched,
    unmatchedParenthesis,
    noSuchSymbol,
    unimplemented,
    missingTerm,
}

ParseResult!T ok(T)(T value)
{
    ParseResult!T result;
    result.type = ParseResultType.ok;
    result.value = value;
    return result;
}


ParseResult!T failCopy(T, TOtherResult)(TOtherResult other)
{
    assert(other.type != ParseResultType.ok);
    ParseResult!T result;
    result.type = other.type;
    result.errorAt = other.errorAt;
    result.symbolName = other.symbolName;
    return result;
}

ParseResult!T fail(T)(ParseResultType type, string errorAt, string symbolName = "")
{
    ParseResult!T result;
    result.type = type;
    result.errorAt = errorAt;
    result.symbolName = symbolName;
    return result;
}

bool isCloseToZero(float a)
{
    import std.math;
    return isClose(a, 0.0f, 0.00001f, 0.00001f); 
}

struct ParseResult(T)
{
    ParseResultType type;
    union
    {
        T value;
        struct { string errorAt; string symbolName; }
    }
}

bool eatSigns()(auto ref string input, out bool sign)
{
    if (input.empty)
    {
        sign = false;
        return false;
    }

    bool matchSign()
    {
        if (input.front == '-')
        {
            sign = !sign;
            input.popFront();
            return true;
        }
        else if (input.front == '+')
        {
            input.popFront();
            return true;
        }
        return false;
    }

    if (!matchSign())
    {
        sign = false;
        return false;
    }

    while (!input.empty && matchSign()){}
    
    return true; 
}
unittest
{
    {
        bool result;
        assert(!eatSigns("", result));
    }
    {
        bool result;
        assert(eatSigns("-", result));
        assert(result);
    }
    {
        bool result;
        string input = "1";
        assert(!eatSigns(input, result));
        assert(input == "1");
    }
    {
        bool result;
        string input = "-1";
        assert(eatSigns(input, result));
        assert(result);
        assert(input == "1");
    }
    {
        bool result;
        string input = "-+-+";
        eatSigns(input, result);
        assert(!result);
    }
}

ParseResult!Expression parseExpression()(auto ref string input, in SymbolTable symbolTable)
{
    string copy = input;

    Term[][] termProducts;
    {
        skipWhitespace(copy);

        if (copy.empty)
            return ok(Expression(termProducts));

        // TODO: care
        bool currentSignMinus;
        do
        {
            Term[] terms;

            if (currentSignMinus)
                terms = [ minusOneTerm ];

            bool invert = false;
            while (!copy.empty)
            {
                if (copy.empty)
                    break;

                bool whetherNegateTerm;   
                eatSigns(copy, whetherNegateTerm);

                auto termResult = parseTerm(copy, symbolTable);
                if (termResult.type != ParseResultType.ok)
                    return failCopy!Expression(termResult);
                
                Term term = termResult.value;
                if (invert)
                {
                    term.power = negate(term.power);
                }
                if (whetherNegateTerm)
                {
                    auto wrapper = Expression([[ minusOneTerm, term, ]]);
                    term = Term(Inner(1.0f), Inner(wrapper));
                }
                terms ~= term;

                skipWhitespace(copy);

                if (copy.empty)
                    break;
                
                if (copy.front == '*')
                {
                    invert = false;
                    copy.popFront();
                    skipWhitespace(copy);
                }
                else if (copy.front == '/')
                {
                    invert = true;
                    copy.popFront();
                    skipWhitespace(copy);
                }
                else
                {
                    break;
                }
            }

            termProducts ~= terms;

            if (copy.empty)
                break;

            skipWhitespace(copy);

            if (copy.empty)
                break;

            if (copy.front == '+')
            {
                currentSignMinus = false;
                copy.popFront();
            }
            else if (copy.front == '-')
            {
                currentSignMinus = true;
                copy.popFront();
            }
            else
            {
                break;
            }

            skipWhitespace(copy);

            if (copy.empty)
                return fail!Expression(ParseResultType.missingTerm, copy);
        }
        while (true);
    }

    input = copy; // @suppress(dscanner.suspicious.auto_ref_assignment)
    return ok(Expression(termProducts));
}
unittest
{
    float[3] a;
    auto symbolTable = createSymbolTableFromVariables!a;

    {
        string input = "123";
        auto r = parseExpression(input, symbolTable);
        assert(r.type == ParseResultType.ok);
        assert(r.value.terms.length == 1);

        auto terms = r.value.terms[0];
        assert(terms.length == 1);

        auto term = terms[0];
        assert(term.inner == Inner(123.0f));
        assert(term.power == Inner(1.0f));
    }
    {
        string input = "123 ^ 6";
        auto r = parseExpression(input, symbolTable);
        assert(r.type == ParseResultType.ok);
        assert(r.value.terms.length == 1);

        auto terms = r.value.terms[0];
        assert(terms.length == 1);

        auto term = terms[0];
        assert(term.inner == Inner(123.0f));
        assert(term.power == Inner(6.0f));
    }
    // {
    //     string input = "-a0 ^ 6";
    //     auto r = parseExpression(input, symbolTable);
    //     assert(r.type == ParseResultType.ok);
    //     assert(r.value.terms.length == 1);

    //     auto terms = r.value.terms[0];
    //     assert(terms.length == 1);

    //     auto expectedTerm = Term(Inner(6.0f), Inner(Symbol(0)));
    //     assert(negate(expectedTerm) == terms[0].inner);
    // }
    {
        string input = "1^(-6)";
        auto r = parseExpression(input, symbolTable);
        assert(r.type == ParseResultType.ok);
        assert(r.value.terms.length == 1);

        auto terms = r.value.terms[0];
        assert(terms.length == 1);
        
        auto term = terms[0];
        assert(term.inner == Inner(1.0f));

        term.power.match!(
            (Expression ex)
            {
                assert(ex.terms.length == 1);
                auto powerTermsProduct = ex.terms[0];
                assert(powerTermsProduct.length == 1);
                auto powerTerm = powerTermsProduct[0];
                assert(powerTerm.power == Inner(1.0f));

                auto app = appender!string;
                assert(powerTerm.inner == Inner(Expression([[minusOneTerm, Term(Inner(1.0f), Inner(6.0f))]])));
            },
            (_)
            {
                assert(0);
            }
        );

    }
}

void skipWhitespace(ref string input)
{
    import std.uni;
    while (!input.empty && isWhite(input.front))
        input.popFront();
}
unittest
{
    {
        string input = "a  b";
        skipWhitespace(input);
        assert(input == "a  b");
    }
    {
        string input = "  b  ";
        skipWhitespace(input);
        assert(input == "b  ");
    }
    {
        string input = "";
        skipWhitespace(input);
        assert(input == "");
    }
}


Inner negate(Inner inner)
{
    return inner.match!(
        (float constant)
        {
            return Inner(-constant);
        },
        (expressionOrSymbol)
        {
            auto term = Term(Inner(1.0f), inner);
            auto expression = Expression([[ minusOneTerm, term, ]]);
            return Inner(expression);
        }
    );
}
unittest
{
    {
        auto i = Inner(1.0f);
        assert(negate(i) == Inner(-1.0f));
    }
    {
        auto i = Inner(-5.0f);
        assert(negate(i) == Inner(5.0f));
    }
    {
        float a = 1.9f;
        auto symbolTable = createSymbolTableFromVariables!a;
        auto i = Inner(symbolTable.find(a.stringof));
        auto neg = negate(i);
        assert(eval(neg, symbolTable) == -a);
    }
    // {
    //     float a = 2.8f;
    //     auto symbolTable = createSymbolTableFromVariables!a;
    //     auto exResult = parseExpression("1 + 2 * a", symbolTable);
    //     assert(exResult.type == ParseResultType.ok);
    //     auto negExpression = negate(exResult.value);
    //     assert(eval(exResult.value, symbolTable) == -eval(negExpression.value, symbolTable));
    // }
}

enum minusOneTerm = Term(Inner(1.0f), Inner(-1.0f));


ParseResult!Inner parseInner()(auto ref string input, in SymbolTable symbolTable)
{
    string copy = input;

    auto ok(Inner t)
    {
        input = copy;
        return .ok(t);
    }

    {
        float number;
        if (parse(copy, number))
            return ok(Inner(number));
    }
    {
        string name;
        if (parseName(copy, name))
        {
            Symbol symbol = symbolTable.find(name);
            if (symbol.index == -1)
                return fail!Inner(ParseResultType.noSuchSymbol, copy, name);
            else
                return ok(Inner(symbol));
        }
    }
    {
        if (copy.front == '(')
        {
            copy.popFront();
            auto expressionParseResult = parseExpression(copy, symbolTable);
            if (expressionParseResult.type != ParseResultType.ok)
                return failCopy!Inner(expressionParseResult);
            
            if (copy.empty || copy.front != ')')
                return fail!Inner(ParseResultType.unmatchedParenthesis, copy);
            
            copy.popFront();
            
            return ok(Inner(expressionParseResult.value));
        }
    }
    return fail!Inner(ParseResultType.notMatched, input);
}
unittest
{
    float[3] a;
    auto symbolTable = createSymbolTableFromVariables!a;

    {
        string input = "1";
        auto r = parseInner(input, symbolTable);
        assert(r.type == ParseResultType.ok);
        assert(r.value == Inner(1));
        assert(input == "");
    }
    {
        string input = "a0";
        auto r = parseInner(input, symbolTable);
        assert(r.type == ParseResultType.ok);
        assert(r.value == Inner(Symbol(0)));
        assert(input == "");
    }
    {
        auto input = "-a0";
        auto r = parseInner(input, symbolTable);
        assert(r.type == ParseResultType.notMatched);
        assert(input == "-a0");
    }
}


ParseResult!Term parseTerm(ref string input, in SymbolTable symbolTable)
{
    string copy = input;

    if (copy.empty)
        return fail!Term(ParseResultType.notMatched, copy);

    auto innerResult = parseInner(copy, symbolTable);
    if (innerResult.type != ParseResultType.ok)
        return failCopy!Term(innerResult);

    skipWhitespace(copy);
    if (copy.empty || copy.front != '^')
    {
        input = copy;
        return ok(Term(Inner(1), innerResult.value));
    }

    copy.popFront();
    skipWhitespace(copy);
    auto powerResult = parseInner(copy, symbolTable);
    if (powerResult.type != ParseResultType.ok)
        return failCopy!Term(powerResult);

    input = copy;
    return ok(Term(powerResult.value, innerResult.value));
}
unittest
{
    float[3] a;
    auto symbolTable = createSymbolTableFromVariables!a;

    {
        string input = "a0";
        auto r = parseTerm(input, symbolTable);
        assert(r.type == ParseResultType.ok);
        assert(input == "");
        assert(r.value.power == Inner(1.0f));
        assert(r.value.inner == Inner(Symbol(0)));
    }
    {
        string input = "a1 ^ 1.2";
        auto r = parseTerm(input, symbolTable);
        assert(r.type == ParseResultType.ok);
        assert(input == "");
        assert(r.value.power == Inner(1.2f));
        assert(r.value.inner == Inner(Symbol(1)));
    }
    {
        string input = "a0 ^ -1.2";
        auto r = parseTerm(input, symbolTable);
        assert(r.type == ParseResultType.ok);
        assert(input == "");
        assert(r.value.power == Inner(-1.2f));
        assert(r.value.inner == Inner(Symbol(0)));
    }
}

bool parseName(ref string input, out string name)
{
    string copy = input;
    import std.uni;

    string getName()
    {
        return input[0 .. cast(size_t)(copy.ptr - input.ptr)];
    }

    if (copy.empty)
        return false;

    if (isAlpha(copy.front) || copy.front == '_')
    {
        copy.popFront();
        
        if (copy.empty)
        {
            name = getName();
            input = copy;
            return true;
        }
    }
    else
    {
        return false;    
    }

    static bool isValidChar(dchar ch)
    {
        return isAlphaNum(ch) || ch == '_';
    }

    while (isValidChar(copy.front))
    {
        copy.popFront();
        if (copy.empty)
            break;
    }

    name = getName();
    input = copy;
    return true;
}

bool parse()(auto ref string input, out float number)
{
    string copy = input;
    bool sign = false;

    if (copy.empty)
        return false;

    if (copy.front == '-')
    {
        sign = true;
        copy.popFront();
        if (copy.empty)
            return false;
    }
    else if (copy.front == '+')
    {
        sign = false;
        copy.popFront();
        if (copy.empty)
            return false;
    }

    import std.uni;
    if (!isNumber(copy.front))
        return false;

    float whole = 0;
    do
    {
        whole *= 10;
        whole += cast(int) copy.front - '0';
        copy.popFront();
    }
    while (!copy.empty && isNumber(copy.front));

    
    if (copy.empty || copy.front != '.')
    {
        number = whole;
        if (sign)
            number = -number;
        input = copy; // @suppress(dscanner.suspicious.auto_ref_assignment)
        return true;
    }
    copy.popFront();

    float fraction = 0;
    int count = 0;
    while (!copy.empty && isNumber(copy.front)) 
    {
        fraction *= 10;
        fraction += cast(int) copy.front - '0';
        count++;
        copy.popFront();
    }

    input = copy; // @suppress(dscanner.suspicious.auto_ref_assignment)
    number = fraction / (10 ^^ count) + whole;
    if (sign)
        number = -number;
    return true;
}
unittest
{
    import std.math;
    {
        float number;
        assert(parse("123", number));
        assert(isClose(number, 123));
    }
    {
        float number;
        assert(parse("123.1", number));
        assert(isClose(number, 123.1f));
    }
    {
        float number;
        assert(parse("123.123", number));
        assert(isClose(number, 123.123f));
    }
}


float eval(const Inner inner, in SymbolTable symbolTable)
{
    return inner.match!(
        (const Expression expression0) => eval(expression0, symbolTable),
        (const Symbol symbol) => *symbolTable.addresses[symbol.index],
        (const float constant) => constant);
}

float eval(const(Expression) expression, in SymbolTable symbolTable)
{
    float result = 0;
    foreach (termProduct; expression.terms)
    {
        float product = 1;
        foreach (term; termProduct)
        {
            float a = eval(term.inner, symbolTable);
            float b = eval(term.power, symbolTable);
            product *= a ^^ b;
        }
        result += product;
    }
    return result;
}

// Assumes the rows are ordered (the first row contains least zeros, etc)
void gaussianSimplify(Matrix : Expression[M][N], size_t N, size_t M)(ref Matrix matrix)
{
    import std.algorithm.comparison : min, max;
    static assert(M >= 2);
    static assert(N >= 1);
    const numColsWithoutRHS = M - 1;

    import std.math;
    // The last column is the rhs of equals.
    foreach (simplificationIndex; 0 .. min(N, numColsWithoutRHS))
    {
        const colStartIndex = simplificationIndex;
        const rowMainIndex = simplificationIndex;
        
        // a b
        // c d
        Expression a = matrix[rowMainIndex][colStartIndex];
        
        foreach (rowIndex; rowMainIndex + 1 .. N)
        {
            ref Expression refc() { return matrix[rowIndex][colStartIndex]; }
            Expression c = refc();
            refc = Expression();
            
            foreach (colIndex; colStartIndex + 1 .. M)
            {
                ref Expression b() { return matrix[rowMainIndex][colIndex]; }
                ref Expression d() { return matrix[rowIndex][colIndex]; }
                
                // a * d + -c * b
                Expression subtraction = Expression([
                    [term(a), term(d)],
                    [minusOneTerm, term(c), term(b)],
                ]);

                d = subtraction;
            }
        }
    }

    // zero out whatever we can.
    {
        const startColIndex = numColsWithoutRHS - 1;
        const maxTouchedRowCoordExclusive = min(N, numColsWithoutRHS);
        const numNonZeroColumns = abs(numColsWithoutRHS - maxTouchedRowCoordExclusive) + 1;
        const startRowIndex = maxTouchedRowCoordExclusive - 1;
        const numIterations = startRowIndex;

        foreach (iterationIndex; 0 .. numIterations)
        {
            const factorRowIndex = startRowIndex - iterationIndex;
            const factorColIndex = startColIndex - iterationIndex;

            /*
                Example:
                a b c = d
                0 e f = g
                
                M = 4, N = 2
                numColsWithoutRHS = 3
                startColIndex = 2        (c, f)
                numIterations = 1
                maxTouchedRowCoordExclusive = 2  (1 past f)
                numNonZeroColumns = 2    (e, f)
                startRowIndex = 1        (0 e f = g)
            */

            // row = 1, col = 2
            Expression f = matrix[factorRowIndex][factorColIndex];

            foreach (rowIndex; 0 .. factorRowIndex)
            {
                ref Expression c() { return matrix[rowIndex][factorColIndex]; }

                // the column of e = (column of f) - (number of elements in (e, f)) + 1
                size_t eColIndex = factorColIndex - numNonZeroColumns + 1;

                // a -> af - 0 = af
                foreach (colIndex; 0 .. eColIndex)
                {
                    ref Expression a() { return matrix[rowIndex][colIndex]; }
                    a = Expression([[term(a), term(f)]]);
                }

                // e
                foreach (colIndex; eColIndex .. factorColIndex)
                {
                    ref Expression b() { return matrix[rowIndex][colIndex]; }
                    ref Expression e() { return matrix[factorRowIndex][colIndex]; }
                    
                    // bf - ec
                    Expression subtraction = Expression([
                        [term(b), term(f)],
                        [minusOneTerm, term(e), term(c)]
                    ]);

                    b = subtraction;
                }
                
                // skip the rest, but the last.
                // those will be zeros by the previous operations.
                {
                    ref Expression d() { return matrix[rowIndex][$ - 1]; }
                    ref Expression g() { return matrix[factorRowIndex][$ - 1]; }
                    
                    // dg - ec
                    Expression subtraction = Expression([
                        [term(d), term(f)],
                        [minusOneTerm, term(g), term(c)]
                    ]);

                    d = subtraction;
                }

                // cf - fc = 0
                c = Expression();
            }
        }
    }
}

alias InnerSimplificationInfo = SumType!(float, Symbol, TermSimplificationResult[]);

InnerSimplificationInfo simplify(Inner a)
{
    return a.match!(
        (Expression expression)
        {
            TermSimplificationResult[] simplifiedExpression = .simplifyInternal(expression);

            // multiplying by 0.
            if (simplifiedExpression.length == 0)
                return InnerSimplificationInfo(0);

            return InnerSimplificationInfo(simplifiedExpression);
        },
        (other) => InnerSimplificationInfo(other)
    );
}

struct ComplicatedTermSimplificationInfo
{
    InnerSimplificationInfo power;
    InnerSimplificationInfo inner;
}

struct TermSimplificationResult
{
    float constant;
    float[] powersOfSymbols;

    // Need to be multiplied.
    // Contain complicated expressions, like (a + b) ^ 2, a^b, 7^(a + b)
    ComplicatedTermSimplificationInfo[] complicated;

    TermSimplificationResult[][] toDistribute;
}

TermSimplificationResult simplify(Term[] termProduct)
{
    import std.algorithm;
    import std.math;

    TermSimplificationResult result;
    result.constant = 1.0f;

    foreach (term; termProduct)
    {
        InnerSimplificationInfo power = .simplify(term.power);
        InnerSimplificationInfo inner = .simplify(term.inner);

        power.match!(
            (float constantPower)
            {
                inner.match!(
                    (float constantInner)
                    {
                        result.constant *= constantInner ^^ constantPower;
                    },
                    (Symbol symbol)
                    {
                        auto oldLength = result.powersOfSymbols.length;
                        if (oldLength <= symbol.index)
                        {
                            result.powersOfSymbols.length = symbol.index + 1;
                            result.powersOfSymbols[oldLength .. $] = 0;
                        }
                        result.powersOfSymbols[symbol.index] += constantPower;
                    },
                    (TermSimplificationResult[] expressionSimplificationInfos)
                    {
                        assert(expressionSimplificationInfos.length != 0);

                        auto notZero = expressionSimplificationInfos.filter!(a => !isCloseToZero(a.constant));
                        if (notZero.empty)
                            return;

                        auto notZeroArray = notZero.array;
                        auto collapsableCount = notZeroArray.count!(a => 
                            isClose(a.constant, 1)
                            || a.complicated.length == 0);
                        
                        if (collapsableCount == notZeroArray.length)
                        {
                            if (notZeroArray.length == 1)
                            {
                                assert(notZeroArray[0].toDistribute.length == 0);
                                distribute(result, notZeroArray[0]);
                            }
                            else
                            {
                                result.toDistribute ~= notZeroArray;
                            }
                            return;
                        }

                        {
                            result.complicated ~= ComplicatedTermSimplificationInfo(
                                 InnerSimplificationInfo(constantPower), InnerSimplificationInfo(notZeroArray));    
                        }
                    }
                );
            },
            (other)
            {
                result.complicated ~= ComplicatedTermSimplificationInfo(
                    InnerSimplificationInfo(other), inner);
            }
        );
    }

    return result;
}

void distribute(ref TermSimplificationResult a, in TermSimplificationResult b)
{
    auto powers = a.powersOfSymbols;
    if (powers.length < b.powersOfSymbols.length)
    {
        auto oldLength = powers.length;
        powers.length = b.powersOfSymbols.length;
        powers[oldLength .. $] = 0;
    }
    powers[0 .. b.powersOfSymbols.length] += b.powersOfSymbols[];
    a.powersOfSymbols = powers;
    a.constant *= b.constant;
    a.complicated ~= b.complicated;
}

TermSimplificationResult[] simplifyInternal(Expression exp)
{
    import std.algorithm;
    import std.math;

    if (exp.terms.length == 0)
        return [];

    TermSimplificationResult[] simplicationResults = exp.terms
        .map!(a => .simplify(a))
        .filter!(a => !isCloseToZero(a.constant))
        .map!((mainInfo)
        {
            if (mainInfo.toDistribute.length == 0)
                return [mainInfo];

            TermSimplificationResult[] newInfos;

            newInfos = mainInfo.toDistribute[0];
            foreach (distributionInfo; newInfos)
            {
                assert(distributionInfo.toDistribute.length == 0);
                distribute(distributionInfo, mainInfo);
            }

            // (a + b) * (c + d) = (ac + ad + bc + bd)
            foreach (distributionInfos; mainInfo.toDistribute[1 .. $])
            {
                auto oldLength = newInfos.length;
                auto newLength = oldLength * distributionInfos.length;
                newInfos.length = newLength;
                foreach (duplicationIndex; 1 .. distributionInfos.length)
                {
                    foreach (sumIndex; 0 .. oldLength)
                    {
                        ref TermSimplificationResult el() { return newInfos[duplicationIndex * oldLength + sumIndex]; }
                        ref TermSimplificationResult source() { return newInfos[sumIndex]; }
                        
                        el.complicated = source.complicated.dup;
                        el.powersOfSymbols = source.powersOfSymbols.dup;
                        el.constant = source.constant;
                    }
                }

                foreach (distributionInfoIndex, ref const distributionInfo; distributionInfos)
                {
                    foreach (newInfoDuplicateIndex; 0 .. oldLength)
                    {
                        distribute(newInfos[distributionInfoIndex * oldLength + newInfoDuplicateIndex], distributionInfo);
                    }
                }

                // omg memory goes brrrr
                newInfos = newInfos.filter!((ref a) => !isCloseToZero(a.constant)).array;
            }

            return newInfos;
        })
        .joiner
        .filter!(a => !isCloseToZero(a.constant))
        .array;


    bool[] scrapped = new bool[](simplicationResults.length);

    // bring together same factors
    foreach (index, ref result0; simplicationResults)
    {
        if (scrapped[index])
            continue;

        foreach (otherIndex, const ref result1; simplicationResults[index + 1 .. $])
        {
            if (result0.powersOfSymbols == result1.powersOfSymbols
                && result0.complicated == result1.complicated)
            {
                scrapped[otherIndex + index + 1] = true;
                result0.constant += result1.constant;
            }
        }
    }

    return iota(0, simplicationResults.length)
        .filter!(i => !scrapped[i])
        .map!(i => simplicationResults[i])
        .filter!(a => !isCloseToZero(a.constant))
        .array;
}

Term[] flatten(TermSimplificationResult a)
{
    Term[] result;

    foreach (symbolIndex, powerOfSymbol; a.powersOfSymbols)
    {
        if (!isCloseToZero(powerOfSymbol))
            result ~= Term(Inner(powerOfSymbol), Inner(Symbol(symbolIndex)));
    }

    foreach (ComplicatedTermSimplificationInfo complicated; a.complicated)
    {
        Term term;
        auto t(InnerSimplificationInfo info)
        {
            return info.match!(
                (TermSimplificationResult[] b)
                {
                    return Inner(Expression(b.map!flatten.array));
                },
                (other) { return Inner(other); }
            );
        }

        term.inner = t(complicated.inner);
        term.power = t(complicated.power);
        result ~= term;
    }
    
    if (!isClose(a.constant, 1) || result.length == 0)
        result ~= term(Inner(a.constant));

    return result;
}

Expression simplify(Expression expression)
{
    auto internal = simplifyInternal(expression);
    return Expression(internal.map!flatten.array);
}

void main()
{
    float[5] k;
    float[3] segments;

    float* a = &segments[0];
    float* b = &segments[1];
    float* c = &segments[2];

    auto symbolTable = createSymbolTableFromVariables!(a, b, c, k);
    assert(symbolTable.names.length == 8);
    import std.stdio;


    void gauss()
    {
        enum N = 4;
        enum M = 6;

        bool ok = true;
        Expression[M] parseRow(string[M] expressions...)
        {
            Expression[M] result;

            foreach (i; 0 .. M)
            {
                string text = expressions[i];
                auto r = parseExpression(text, symbolTable);
                if (r.type != ParseResultType.ok)
                {
                    writeln("Error ", r.type, " at ", r.errorAt, "; ", r.symbolName);
                    ok = false;
                }
                result[i] = r.value;
            }
            return result;
        }

        Expression[M] parseSplitRow(string row)
        {
            import std.string;
            return parseRow(row.split(";")[0 .. M]);
        }

        Expression[M][N] parseAll(string all)
        {
            import std.string;
            import std.algorithm;

            return all
                .split("\n")
                .map!(a => a.strip)
                .filter!(l => l.length > 0)
                .map!(a => parseSplitRow(a))
                .array[0 .. N];
        }

        auto p = `
            a^4 / 12; a^3 / 6; a^2 / 2; a; 1; 0
            b^4 / 12; b^3 / 6; b^2 / 2; b; 1; 1
            c^4 / 12; c^3 / 6; c^2 / 2; c; 1; 0
            b^3 / 3;  b^2 / 2; b;       1; 0; 0
        `;

        Expression[M][N] expressions = parseAll(p);
        gaussianSimplify(expressions);

        foreach (ref es; expressions)
        {
            foreach (ref e; es)
            {
                e = simplify(e);
            }
        }

        auto app = appender!string;
        foreach (i; 0 .. N)
        { 
            foreach (j; 0 .. M)
            {
                print(app, symbolTable, expressions[i][j]);
                app ~= ";";
            }
            app ~= "\n";
        }
        writeln(app[]);
    }

    void simplification()
    {
        // auto res = parseExpression("1 * 2 * 3 * 4 * a * a * b * a ^ 2 + 80 * a ^ 4 * b", symbolTable);
        auto res = parseExpression("(1 + 2 + a^3) * (7 + b^3)", symbolTable);
        assert(res.type == ParseResultType.ok);

        auto simplified = simplify(res.value);
        auto app = appender!string;
        print(app, symbolTable, simplified);
        writeln(app[]);
    }

    gauss();

}
