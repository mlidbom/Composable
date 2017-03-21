using System;
using System.Collections.Concurrent;
using FluentAssertions;
using NUnit.Framework;
// ReSharper disable All

namespace Composable.Tests
{
    [TestFixture] public class Csharp7Features
    {
        [Test] public void Out_variables()
        {
            if(TryGet(out int val1, out var val2))
            {
                Console.WriteLine($"{val1}, {val2}");
            }
        }

        [Test] public void Pattern_matching_switch()
        {
            Switch_on_type_pattern(1)
                .Should()
                .Be("1");

            Switch_on_type_pattern("something")
                .Should()
                .Be("something");
        }

        [Test] public void Pattern_matching_if_clauses()
        {
            If_clauses_on_type_patterns(1)
                .Should()
                .Be("1");

            If_clauses_on_type_patterns("something")
                .Should()
                .Be("something");
        }

        [Test] public void Tuples_with_named_members()
        {
            GetName()
                .ForeName.Should()
                .Be("ForeName");
            GetName()
                .MiddleName.Should()
                .Be("MiddleName");
            GetName()
                .SurName.Should()
                .Be("SurName");
        }

        [Test] public void Tuple_deconstruction()
        {
            var (first, middle, last) = GetName();
            first.Should()
                 .Be("ForeName");
            middle.Should()
                  .Be("MiddleName");
            last.Should()
                .Be("SurName");

            //Deconstructing assignment:
            (first, middle, last) = GetName();

            first.Should()
                 .Be("ForeName");
            middle.Should()
                  .Be("MiddleName");
            last.Should()
                .Be("SurName");
        }

        [Test] public void Deconstructing_type()
        {
            var (x, y) = new DeconstructingPoint(1, 2);
            x.Should()
             .Be(1);
            y.Should()
             .Be(2);
        }

        [Test] public void Deconstructing_type_through_extension()
        {
            var (x, y) = new Point(1, 2);
            x.Should()
             .Be(1);
            y.Should()
             .Be(2);
        }

        [Test] public void Local_function()
        {
            string ToString(int x) => x.ToString();

            int ToInt(string x) { return int.Parse(x); }

            ToString(2)
                .Should()
                .Be("2");

            ToInt("3")
                .Should()
                .Be(3);
        }

        [Test] public void Ref_return_values()
        {
            ref int Find(int number, int[] numbers)
            {
                for (int i = 0; i < numbers.Length; i++)
                {
                    if (numbers[i] == number)
                    {
                        return ref numbers[i]; // return the storage location, not the value
                    }
                }
                throw new IndexOutOfRangeException($"{nameof(number)} not found");
            }

            int[] array = { 0, 1, 2,3,4,5,6,7,8,9 };
            ref int place = ref Find(4, array); // aliases 7's place in the array
            array[4]
                .Should()
                .Be(4);
            place = 9; // replaces 7 with 9 in the array
            array[4]
                .Should()
                .Be(9);

            array[3]
                .Should()
                .Be(3);

            Find(3, array) = 99;

            array[3]
                .Should()
                .Be(99);
        }

        [Test] public void Binary_literals()
        {
            0b1.Should()
               .Be(1);

            0b10.Should()
                .Be(2);

            0b11.Should()
                .Be(3);

            0b100.Should()
                 .Be(4);

            0b101.Should()
                 .Be(5);

            0b110.Should()
                 .Be(6);

            0b111.Should()
                 .Be(7);
        }

        class PersonWithExpressionBodiesConstructorDestructorAndProperties
        {
            private static ConcurrentDictionary<int, string> _names = new ConcurrentDictionary<int, string>();
            private int _id = GetId();
            static int GetId() => 1;

            public PersonWithExpressionBodiesConstructorDestructorAndProperties(string name) => _names.TryAdd(_id, name); // constructors
            ~PersonWithExpressionBodiesConstructorDestructorAndProperties() => _names.TryRemove(_id, out string _);              // destructors
            public string Name
            {
                get => _names[_id];                                 // getters
                set => _names[_id] = value;                         // setters
            }
        }

        class PersonWithThrowExceptionExpressions
        {
            public string Name { get; }
            public PersonWithThrowExceptionExpressions(string name) => Name = name ?? throw new ArgumentNullException(nameof(name));
            public string GetFirstName()
            {
                var parts = Name.Split(' ');
                return (parts.Length > 0) ? parts[0] : throw new InvalidOperationException("No name!");
            }
            public string GetLastName() => throw new NotImplementedException();
        }

        static (string ForeName, string MiddleName, string SurName) GetName() => ("ForeName", "MiddleName", "SurName");

        static string If_clauses_on_type_patterns(object value)
        {
            if(value is int val)
            {
                return val.ToString();
            }

            if(value is string sval)
            {
                return sval;
            }
            throw new ArgumentException();
        }

        static string Switch_on_type_pattern(object value)
        {
            switch(value)
            {
                case int i:
                    return i.ToString();
                case string s:
                    return s;
                default:
                    throw new ArgumentException();
            }
        }

        static bool TryGet(out int val1, out string val2)
        {
            val1 = 1;
            val2 = 2.ToString();
            return true;
        }

        internal class Point
        {
            public Point(int x, int y)
            {
                Y = y;
                X = x;
            }
            public int X { get; }
            public int Y { get; }
        }

        class DeconstructingPoint
        {
            public DeconstructingPoint(int x, int y)
            {
                Y = y;
                X = x;
            }
            public int X { get; }
            public int Y { get; }

            public void Deconstruct(out int x, out int y)
            {
                x = this.X;
                y = this.Y;
            }
        }
    }

    static class PointDeconstructor
    {
        public static void Deconstruct(this Csharp7Features.Point @this, out int x, out int y)
        {
            x = @this.X;
            y = @this.Y;
        }
    }
}
