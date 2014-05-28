﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExpressionToCodeLib;
using ValueUtils;
using Xunit;

namespace ValueUtilsTest {
    public class FieldwiseHasherTest {

        static readonly Func<SampleStruct, int> hash = FieldwiseHasher<SampleStruct>.Instance;

        [Fact]
        public void IdenticalValuesHaveIdenticalHashes() {
            PAssert.That(() =>
                hash(new SampleStruct(1, 2, "3", 4))
                == hash(new SampleStruct(1, 2, "3", 4)));
        }


        [Fact]
        public void IsNotJustAWrapperAroundGetHashCode() {
            PAssert.That(() =>
                hash(new SampleStruct(1, 2, "3", 4))
                != new SampleStruct(1, 2, "3", 4).GetHashCode());
        }

        [Fact]
        public void OneDifferentValueMemberChangesHash() {
            PAssert.That(() =>
                hash(new SampleStruct(1, 2, "3", 4))
                != hash(new SampleStruct(11, 2, "3", 4)));
            PAssert.That(() =>
                hash(new SampleStruct(1, 2, "3", 4))
                != hash(new SampleStruct(1, 12, "3", 4)));
            PAssert.That(() =>
                hash(new SampleStruct(1, 2, "3", 4))
                != hash(new SampleStruct(1, 2, "13", 4)));
            PAssert.That(() =>
                hash(new SampleStruct(1, 2, "3", 4))
                != hash(new SampleStruct(1, 2, "3", 14)));
        }

        [Fact]
        public void IdenticalObjectsHaveIdenticalHashes() {
            //it's important that this is a class not struct instance so we've checked that
            //also, that means we're accessing another assemblies private fields
            PAssert.That(() =>
                FieldwiseHasher.Hash(Tuple.Create(1, 2, "3", 4))
                == FieldwiseHasher.Hash(Tuple.Create(1, 2, "3", 4)));
        }

        [Fact]
        public void OneDifferentObjectMemberChangesHash() {
            PAssert.That(() =>
                FieldwiseHasher.Hash(Tuple.Create(1, 2, "3", 4))
                != FieldwiseHasher.Hash(Tuple.Create(11, 2, "3", 4)));
            PAssert.That(() =>
                FieldwiseHasher.Hash(Tuple.Create(1, 2, "3", 4))
                != FieldwiseHasher.Hash(Tuple.Create(1, 12, "3", 4)));
            PAssert.That(() =>
                FieldwiseHasher.Hash(Tuple.Create(1, 2, "3", 4))
                != FieldwiseHasher.Hash(Tuple.Create(1, 2, "13", 4)));
            PAssert.That(() =>
                FieldwiseHasher.Hash(Tuple.Create(1, 2, "3", 4))
                != FieldwiseHasher.Hash(Tuple.Create(1, 2, "3", 14)));
        }

        [Fact]
        public void AutoPropsAffectHash() {
            PAssert.That(() =>
                FieldwiseHasher.Hash(new SampleClass { AutoPropWithPrivateBackingField = "x" })
                == FieldwiseHasher.Hash(new SampleClass { AutoPropWithPrivateBackingField = "x" }));
            PAssert.That(() =>
                FieldwiseHasher.Hash(new SampleClass { AutoPropWithPrivateBackingField = "x" })
                != FieldwiseHasher.Hash(new SampleClass { AutoPropWithPrivateBackingField = "y" }));
        }

        [Fact]
        public void TypeMattersAtCompileTime() {
            PAssert.That(() =>
                FieldwiseHasher.Hash(new SampleClass {  AnEnum = SampleEnum.Q })
                != FieldwiseHasher.Hash(new SampleSubClass { AnEnum = SampleEnum.Q }));
        }

        [Fact]
        public void TypeDoesNotMatterAtRunTime() {
            var hasher = FieldwiseHasher<SampleClass>.Instance;
            PAssert.That(() =>
                hasher(new SampleClass { AnEnum = SampleEnum.Q })
                == hasher(new SampleSubClass { AnEnum = SampleEnum.Q }));
        }

        
        [Fact]
        public void SubClassesCheckBaseClassFields() {
            PAssert.That(() =>
                FieldwiseHasher.Hash(new SampleSubClassWithFields { AnEnum = SampleEnum.Q })
                != FieldwiseHasher.Hash(new SampleSubClassWithFields { AnEnum = SampleEnum.P }));
            PAssert.That(() =>
                FieldwiseHasher.Hash(new SampleSubClassWithFields { AnEnum = SampleEnum.Q })
                == FieldwiseHasher.Hash(new SampleSubClassWithFields { AnEnum = SampleEnum.Q }));
        }

        [Fact]
        public void NonCyclicalSelfReferentialTypesWork() {
            PAssert.That(() =>
                FieldwiseHasher.Hash(new SampleClass { SelfReference = new SampleClass { AnEnum = SampleEnum.Q } })
                != FieldwiseHasher.Hash(new SampleClass { SelfReference = new SampleClass { AnEnum = SampleEnum.P } }));
            PAssert.That(() =>
                FieldwiseHasher.Hash(new SampleClass { SelfReference = new SampleClass { AnEnum = SampleEnum.Q } })
                != FieldwiseHasher.Hash(new SampleClass { SelfReference = new SampleClass { AnEnum = SampleEnum.Q } }));
        }
    }
}
