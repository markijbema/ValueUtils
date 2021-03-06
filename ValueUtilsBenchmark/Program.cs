﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ExpressionToCodeLib;
using MoreLinq;

namespace ValueUtilsBenchmark {
    class Program {

        //Note: the manual hash implementations are bastardized FNV since nobody in their right mind
        // would manually split field hash codes into octets and seperately hash those with FNV.
        // perhaps this makes it less robust, but I know of no better alternative.
        static void Main(string[] args) {
            var complicatedTable = HashAnalysisResult.ToTable(
                "Realistic scenario with an enum, a string, a DateTime, an int? and 3 int fields.", 
                BenchmarkComplicatedCases()
                );
            var intpairTable = HashAnalysisResult.ToTable(
                "A simple pair of ints",
                BenchmarkIntPairCases()
                );
            var duplicationTable = HashAnalysisResult.ToTable(
                "Two ints with both the same value",
                BenchmarkDuplicatePairCases()
                );
            var symmetricalTable = HashAnalysisResult.ToTable(
                "Two ints such that (x,y) is present iif (y,x) is present in the dataset",
                BenchmarkSymmetricalCases()
                );
            var nestedTable = HashAnalysisResult.ToTable(
                "A reference to the type itself and two int fields.  The dataset contains exactly one level of nesting such that the outer object is (x,y) when the inner is (y,x).",
                BenchmarkNastyNestedCases()
                );

            var tables = 
                new XElement("div",
                    complicatedTable, intpairTable, duplicationTable, symmetricalTable, nestedTable);
            Console.WriteLine(tables.ToString());
            tables.Save("BenchResults.html");
        }

        static IEnumerable<HashAnalysisResult> BenchmarkNastyNestedCases() {
            var manuals = (
                from a in MoreEnumerable.Generate(-10000, a => a + 6).TakeWhile(a => a < 14100)
                from b0 in MoreEnumerable.Generate(int.MaxValue, c => (int)(c * 0.95)).TakeWhile(c => c > 0)
                from sign in new[] { -1, 1 }
                let b = sign * b0
                select new NastyNestedManual {
                    Nested = new NastyNestedManual {
                        A = b,
                        B = a,
                    },
                    A = a,
                    B = b,
                }
            ).ToArray();

            var valueObjects = manuals.Select(m => m.ToValueObject()).ToArray();
            var tuples = manuals.Select(m => m.ToTuple()).ToArray();

            yield return ProcessList(manuals);
            yield return ProcessList(valueObjects);
            yield return ProcessList(tuples);
        }


        static IEnumerable<HashAnalysisResult> BenchmarkIntPairCases() {
            var manuals = (
                from a in MoreEnumerable.Generate(-8000, a => a + 4).TakeWhile(a => a < 8000)
                from b0 in MoreEnumerable.Generate(int.MaxValue, c => (int)(c * 0.95)).TakeWhile(c => c > 0)
                from sign in new[] { -1, 1 }
                let b = sign * b0
                select new IntPairManual {
                    A = a,
                    B = b,
                }
            ).ToArray();

            var valueObjects = manuals.Select(m => m.ToValueObject()).ToArray();
            var tuples = manuals.Select(m => m.ToTuple()).ToArray();
            var structs = manuals.Select(m => m.ToStruct()).ToArray();
            var anonymous = manuals.Select(m =>
              new {
                  m.A,
                  m.B,
              }).ToArray();

            yield return ProcessList(manuals);
            yield return ProcessList(valueObjects);
            yield return ProcessList(tuples);
            yield return ProcessList(structs);
            yield return ProcessList(anonymous);
        }


        static IEnumerable<HashAnalysisResult> BenchmarkSymmetricalCases() {
            var manuals = (
                from a in MoreEnumerable.Generate(-5100, a => a + 5).TakeWhile(a => a < 5100)
                from b0 in MoreEnumerable.Generate(int.MaxValue, c => (int)(c * 0.95)).TakeWhile(c => c > 0)
                from sign in new[] { -1, 1 }
                let b = sign * b0
                from swap in new[] { true, false }
                select new IntPairManual {
                    A = swap ? a : b,
                    B = swap ? b : a,
                }
            ).Distinct().ToArray();

            var valueObjects = manuals.Select(m => m.ToValueObject()).ToArray();
            var tuples = manuals.Select(m => m.ToTuple()).ToArray();
            var structs = manuals.Select(m => m.ToStruct()).ToArray();
            var anonymous = manuals.Select(m =>
              new {
                  m.A,
                  m.B,
              }).ToArray();

            yield return ProcessList(manuals);
            yield return ProcessList(valueObjects);
            yield return ProcessList(tuples);
            yield return ProcessList(structs);
            yield return ProcessList(anonymous);
        }

        static IEnumerable<HashAnalysisResult> BenchmarkDuplicatePairCases() {
            var manuals = (
                from a in Enumerable.Range(0, 3000000)
                select new IntPairManual {
                    A = a,
                    B = a,
                }
            ).ToArray();

            var valueObjects = manuals.Select(m => m.ToValueObject()).ToArray();
            var tuples = manuals.Select(m => m.ToTuple()).ToArray();
            var structs = manuals.Select(m => m.ToStruct()).ToArray();
            var anonymous = manuals.Select(m =>
                new {
                    m.A,
                    m.B,
                }).ToArray();

            yield return ProcessList(manuals);
            yield return ProcessList(valueObjects);
            yield return ProcessList(tuples);
            yield return ProcessList(structs);
            yield return ProcessList(anonymous);
        }

        static IEnumerable<HashAnalysisResult> BenchmarkComplicatedCases() {
            var manuals = (
                from a in MoreEnumerable.Generate(-500, a => a + 4).TakeWhile(a => a < 500)
                from b0 in MoreEnumerable.Generate(int.MaxValue, b => (int)(b * 0.9)).TakeWhile(b => b > 0)
                from sign in new[] { -1, 1 }
                let b = sign * b0
                from c in MoreEnumerable.Generate(1, c => c << 1).TakeWhile(c => c > 0)
                select new ComplicatedManual {
                    AnEnum = (SeekOrigin)(c % 3),
                    A = a,
                    NullableInt = a < b && b < c ? default(int?) : a & b & c,
                    B = b,
                    Label = Math.Min(a, c).ToString(),
                    Time = new DateTime(2014, 1, 1) + TimeSpan.FromSeconds(a + c + Math.Log(b0)),
                    C = c,
                }
            ).ToArray();
            var valueObjects = manuals.Select(m => m.ToValueObject()).ToArray();
            var tuples = manuals.Select(m => m.ToTuple()).ToArray();
            var structs = manuals.Select(m => m.ToStruct()).ToArray();
            var anonymous = manuals.Select(m =>
                new {
                    m.AnEnum,
                    m.A,
                    m.NullableInt,
                    m.B,
                    m.Label,
                    m.Time,
                    m.C
                }).ToArray();

            yield return ProcessList(manuals);
            yield return ProcessList(valueObjects);
            yield return ProcessList(tuples);
            yield return ProcessList(structs);
            yield return ProcessList(anonymous);
        }

        static HashAnalysisResult ProcessList<T>(T[] objs) {
            string name = ObjectToCode.GetCSharpFriendlyTypeName(typeof(T));
            if (name.StartsWith("Tuple"))
                name = "Tuple";
            else if (name.Contains("AnonymousType"))
                name = "Anonymous Type";
            var collisions = AnalyzeHashCollisions(objs);
            return new HashAnalysisResult {
                Name = name,
                Collisions = collisions,
                GetHashCodeNS = TimeInNS(() => {
                    foreach (var inst in objs)
                        inst.GetHashCode();
                }) / objs.Length,
                EqualsNS = TimeInNS(() => {
                    for (int i = 0; i < objs.Length; i++) {
                        objs[i].Equals(objs[i]);
                        objs[i].Equals(objs[(i + 1) % objs.Length]);
                    }
                }) / objs.Length / 2,
                DistinctCountNS = collisions.Rate > 0.99 ? double.NaN : TimeInNS(() => {
                    objs.Distinct().Count();
                }) / objs.Length,
                DictionaryNS = collisions.Rate > 0.99 ? double.NaN : TimeInNS(() => {
                    int idx = 0;
                    objs.ToDictionary(o => o, _ => idx++);
                }) / objs.Length,
            };
        }
        static CollisionStats AnalyzeHashCollisions<T>(T[] objs) {
            return new CollisionStats {
                DistinctValues = objs.Length,
                DistinctHashCodes = objs.Select(o => o.GetHashCode()).Distinct().Count()
            };
        }

        static double TimeInNS(Action action) {
            var initial = Stopwatch.StartNew();
            var timings =
                Enumerable.Range(0, 1000000)
                .Select(_ => {
                    var sw = Stopwatch.StartNew();
                    action();
                    return sw.Elapsed.TotalMilliseconds;
                })
                .TakeUntil(t => initial.ElapsedMilliseconds >= 10000)
                .OrderBy(t => t)
                .ToArray();
            return 1000000.0*timings.Take((timings.Length + 3) / 4).Average();
        }
    }
}
