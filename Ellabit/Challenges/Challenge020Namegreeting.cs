﻿namespace Ellabit.Challenges
{
    public class Challenge020Namegreeting : IChallenge
    {
        public string? Header { get; set; } = "Name greeting!";
        public string? Code { get; set; } = @"
using System;

namespace Ellabit;

public class Challenge
{
		public  string HelloName(string name)
		{
			
		}
}

";
        public string? TestCode { get; set; } = @"
using System;

namespace Ellabit;

public class TestChallenge
{
    public (bool pass, string message) Test1()
    {
        var tmp = new Challenge();
        string sumResult;
        try 
        {
            sumResult = tmp.HelloName(""Gerald"");
        } catch (Exception ex) 
        {
            return (false, ex.ToString() + "" "" + ex.Message);
        }
        return (sumResult == ""Hello Gerald!"",  $""returned: {sumResult}  expected: Hello Gerald!"");
    }
    public (bool pass, string message) Test2()
    {
        var tmp = new Challenge();
        string sumResult;
        try 
        {
            sumResult = tmp.HelloName(""Tiffany"");
        } catch (Exception ex) 
        {
            return (false, ex.ToString() + "" "" + ex.Message);
        }
        return (sumResult == ""Hello Tiffany!"",  $""returned: {sumResult}  expected: Hello Tiffany!"");
    }
    public (bool pass, string message) Test3()
    {
        var tmp = new Challenge();
        string sumResult;
        try 
        {
            sumResult = tmp.HelloName(""Ed"");
        } catch (Exception ex) 
        {
            return (false, ex.ToString() + ""\n"" + ex.Message);
        }
        return (sumResult == ""Hello Ed!"",  $""returned: {sumResult}  expected: Hello Ed!"");
    }
}
";
        public string? Description { get; set; } = @"Create a function that takes a name and returns a greeting in the form of a string.

examples
HelloName(""Gerald"") ➞ ""Hello Gerald!""

HelloName(""Tiffany"") ➞ ""Hello Tiffany!""

HelloName(""Ed"") ➞ ""Hello Ed!""
Notes
The input is always a name(as string).
Don't forget the exclamation mark!";
        public List<string> Tests { get; set; } = new string[] { "Test1", "Test2", "Test3" }.ToList();
    }
}
