import std.sumtype;
import std.stdio;
import std.range;

struct SymbolTable
{
    string[] names;
    float*[] addresses;

    Symbol find(string name) const
    {
        import std.algorithm;
        auto sorted = assumeSorted(names[]);
        auto t = sorted.trisect(name);
        if (t[1].length == 0)
            return Symbol(-1);
        return Symbol(t[0].length);
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

struct Equation
{
    // sum of product of terms
    Term[][] terms;
}

alias Inner = SumType!(Equation, Symbol, float);

struct Term
{
    Inner power;
    Inner inner;
}

import std.array;
import std.format;

void print(TAppender)(ref TAppender appender, in SymbolTable symbolTable, const(Equation) equation)
{
    foreach (termProductIndex, termProduct; equation.terms)
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
                    (const Equation equation0)
                    {
                        appender ~= "(";
                        print(appender, symbolTable, equation0),
                        appender ~= ")";
                    },
                    (const Symbol symbol)
                    {
                        appender ~= symbolTable.names[symbol.index];
                    },
                    (const float constant)
                    {
                        appender.formattedWrite!"%f"(constant);
                    }
                );
            }

            term.power.match!(
                (const Equation equation0)
                {
                    matchInner();
                    appender ~= " ^ (";
                    print(appender, symbolTable, equation0);
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
                        appender.formattedWrite!" ^ %f"(constant); 
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


struct ParseResult(T)
{
    ParseResultType type;
    union
    {
        T value;
        struct { string errorAt; string symbolName; }
    }
}

bool eatSigns(auto ref string input, out bool sign)
{
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

ParseResult!Equation parseEquation()(auto ref string input, in SymbolTable symbolTable)
{
    string copy = input;

    Term[][] termProducts;
    {
        skipWhitespace(copy);

        if (copy.empty)
            return ok(Equation(termProducts));

        // TODO: care
        bool whetherNegate;
        do
        {
            Term[] terms;

            if (whetherNegate)
                terms = [ minusOneTerm ];

            bool invert = false;
            while (!copy.empty)
            {
                if (copy.empty)
                    break;

                bool whetherNegateTerm;   
                eatSigns(input, whetherNegateTerm);

                auto termResult = parseTerm(copy, symbolTable);
                if (termResult.type != ParseResultType.ok)
                    return failCopy!Equation(termResult);
                
                Term term = termResult.value;
                if (invert)
                {
                    term.power = negate(term.power);
                }
                if (whetherNegateTerm)
                {
                    auto wrapper = Equation([[ minusOneTerm, term, ]]);
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
                whetherNegate = false;
                copy.popFront();
            }
            else if (copy.front == '-')
            {
                whetherNegate = true;
                copy.popFront();
            }
            else
            {
                break;
            }

            skipWhitespace(copy);

            if (copy.empty)
                return fail!Equation(ParseResultType.missingTerm, copy);
        }
        while (true);
    }

    input = copy; // @suppress(dscanner.suspicious.auto_ref_assignment)
    return ok(Equation(termProducts));
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
        (equationOrSymbol)
        {
            auto term = Term(Inner(1.0f), inner);
            auto equation = Equation([[ minusOneTerm, term, ]]);
            return Inner(equation);
        }
    );
}
unittest
{
    {
        auto i = Inner(1.0f);
        assert(negate(i) == 1.0f);
    }
    {
        auto i = Inner(-5.0f);
        assert(negate(i) == 5.0f);
    }
    {
        float a = 1.9f;
        auto symbolTable = createSymbolTableFromVariables!a;
        auto i = Inner(symbolTable.find(a.stringof));
        auto neg = negate(i);
        eval(neg, symbolTable) == -a;
    }
    {
        float a = 2.8f;
        auto symbolTable = createSymbolTableFromVariables!a;
        auto eqResult = parseEquation("1 + 2 * a", symbolTable);
        assert(eqResult.type == ParseResultType.ok);
        auto negEquation = negate(eqResult.value);
        assert(eval(eqResult.value, symbolTable) == -eval(negEquation.value, symbolTable));
    }
}

enum minusOneTerm = Term(Inner(1.0f), Inner(-1.0f));


ParseResult!Inner parseInner(ref string input, in SymbolTable symbolTable)
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
            writeln("Finding ", name);
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
            auto equationParseResult = parseEquation(copy, symbolTable);
            if (equationParseResult.type != ParseResultType.ok)
                return failCopy!Inner(equationParseResult);
            
            if (copy.empty || copy.front != ')')
                return fail!Inner(ParseResultType.unmatchedParenthesis, copy);
            
            copy.popFront();
            
            return ok(Inner(equationParseResult.value));
        }
    }
    return fail!Inner(ParseResultType.notMatched, input);
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

bool parse(ref string input, out float number)
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
        input = copy;
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

    input = copy;
    number = fraction / (10 ^^ count) + whole;
    if (sign)
        number = -number;
    return true;
}


float eval(const Inner inner, in SymbolTable symbolTable)
{
    return inner.match!(
        (const Equation equation0) => eval(equation0, symbolTable),
        (const Symbol symbol) => *symbolTable.addresses[symbol.index],
        (const float constant) => constant);
}

float eval(const(Equation) equation, in SymbolTable symbolTable)
{
    float result = 0;
    foreach (termProduct; equation.terms)
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

void main()
{
    float[5] k;
    float[3] segments;

    float* a = &segments[0];
    float* b = &segments[1];
    float* c = &segments[2];

    auto symbolTable = createSymbolTableFromVariables!(k, a, b, c);
    assert(symbolTable.names.length == 8);
    import std.stdio;

    {
        float number;
        string input = "123.123";
        writeln(parse(input, number));
        writeln(number);
    }

    void stuff(string input)
    {
        auto eq = parseEquation(input, symbolTable);
        if (eq.type == ParseResultType.ok)
        {
            auto app = appender!string;
            print(app, symbolTable, eq.value);
            writeln(app[]);
        }
    }
        
    stuff("a + b + c");
    stuff("a^2 + b^3 + c^4");
    stuff("a * b ^ 2 * 3 / 7 * c - b ^ 3");

    {
        auto eq = parseEquation("a^2 + b^(2 * a) - c ^ (-2)", symbolTable);

        if (eq.type == ParseResultType.ok)
        {
            *a = 2;
            *b = 2;
            *c = 3;
            auto t = (*a)^^2.0f + (*b )^^ (2.0f * *a) - (*c) ^^ (-2.0f);
            writeln(t);
            writeln(eq.value.eval(symbolTable));

            auto app = appender!string;
            print(app, symbolTable, eq.value);
            writeln(app[]);
        }
        else
        {
            writeln(eq.type);
            writeln(eq.errorAt);
            writeln(eq.symbolName);
        }
    }
}
