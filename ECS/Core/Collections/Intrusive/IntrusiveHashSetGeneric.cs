﻿
namespace ME.ECS.Collections {

    public struct IntrusiveHashSetBucketGeneric<T> : IStructComponent where T : struct, System.IEquatable<T> {

        public IntrusiveListGeneric<T> list;

    }

    public interface IIntrusiveHashSetGeneric<T> where T : struct, System.IEquatable<T> {

        int Count { get; }
        
        void Add(in T entityData);
        bool Remove(in T entityData);
        int RemoveAll(in T entityData);
        void Clear();
        bool Contains(in T entityData);

        BufferArray<T> ToArray();
        IntrusiveHashSetGeneric<T>.Enumerator GetEnumerator();

    }
    
    #if ECS_COMPILE_IL2CPP_OPTIONS
    [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.DivideByZeroChecks, false)]
    #endif
    public struct IntrusiveHashSetGeneric<T> : IIntrusiveHashSetGeneric<T> where T : struct, System.IEquatable<T> {

        #if ECS_COMPILE_IL2CPP_OPTIONS
        [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.DivideByZeroChecks, false)]
        #endif
        public struct Enumerator : System.Collections.Generic.IEnumerator<T> {

            private IntrusiveHashSetGeneric<T> hashSet;
            private int bucketIndex;
            private IntrusiveListGeneric<T>.Enumerator listEnumerator;
            public T Current => this.listEnumerator.Current;

            [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public Enumerator(IntrusiveHashSetGeneric<T> hashSet) {

                this.hashSet = hashSet;
                this.bucketIndex = 0;
                this.listEnumerator = default;
                
            }

            [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() {

                while (this.bucketIndex <= this.hashSet.buckets.Length) {

                    if (this.listEnumerator.MoveNext() == true) {

                        return true;

                    }

                    var bucket = this.hashSet.buckets[this.bucketIndex];
                    if (bucket.IsAlive() == true) {

                        var node = bucket.GetData<IntrusiveHashSetBucketGeneric<T>>();
                        this.listEnumerator = node.list.GetEnumerator();
                    
                    }
                    ++this.bucketIndex;

                }

                return false;

            }

            [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void Reset() {
                
                this.bucketIndex = 0;
                this.listEnumerator = default;

            }

            object System.Collections.IEnumerator.Current {
                get {
                    throw new AllocationException();
                }
            }

            public void Dispose() {

            }

        }

        private StackArray10<Entity> buckets;
        private int count;

        public int Count => this.count;

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() {

            return new Enumerator(this);

        }

        /// <summary>
        /// Put entity data into array.
        /// </summary>
        /// <returns>Buffer array from pool</returns>
        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public BufferArray<T> ToArray() {

            var arr = PoolArray<T>.Spawn(this.count);
            var i = 0;
            foreach (var entity in this) {
                
                arr.arr[i++] = entity;
                
            }

            return arr;

        }

        /// <summary>
        /// Find an element.
        /// </summary>
        /// <param name="entityData"></param>
        /// <returns>Returns TRUE if data was found</returns>
        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool Contains(in T entityData) {
            
            IntrusiveHashSetGeneric<T>.Initialize(ref this);

            var bucket = (entityData.GetHashCode() & 0x7fffffff) % this.buckets.Length;
            var bucketEntity = this.buckets[bucket];
            if (bucketEntity.IsAlive() == false) return false;
            
            ref var bucketList = ref bucketEntity.GetData<IntrusiveHashSetBucketGeneric<T>>();
            return bucketList.list.Contains(entityData);

        }
        
        /// <summary>
        /// Clear the list.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void Clear() {

            for (int i = 0; i < this.buckets.Length; ++i) {

                var bucket = this.buckets[i];
                if (bucket.IsAlive() == true) {
                    
                    ref var data = ref bucket.GetData<IntrusiveHashSetBucketGeneric<T>>();
                    data.list.Clear();
                    
                }

            }
            
            this.count = 0;

        }

        /// <summary>
        /// Remove data from list.
        /// </summary>
        /// <param name="entityData"></param>
        /// <returns>Returns TRUE if data was found</returns>
        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool Remove(in T entityData) {

            IntrusiveHashSetGeneric<T>.Initialize(ref this);

            var bucket = (entityData.GetHashCode() & 0x7fffffff) % this.buckets.Length;
            var bucketEntity = this.buckets[bucket];
            if (bucketEntity.IsAlive() == false) return false;
            
            ref var bucketList = ref bucketEntity.GetData<IntrusiveHashSetBucketGeneric<T>>(false);
            if (bucketList.list.Remove(entityData) == true) {

                --this.count;
                return true;

            }
            
            return false;
            
        }

        /// <summary>
        /// Remove all nodes data from list.
        /// </summary>
        /// <param name="entityData"></param>
        /// <returns>Returns TRUE if data was found</returns>
        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public int RemoveAll(in T entityData) {
            
            IntrusiveHashSetGeneric<T>.Initialize(ref this);
            
            var bucket = (entityData.GetHashCode() & 0x7fffffff) % this.buckets.Length;
            var bucketEntity = this.buckets[bucket];
            if (bucketEntity.IsAlive() == false) return 0;
            
            ref var bucketList = ref bucketEntity.GetData<IntrusiveHashSetBucketGeneric<T>>(false);
            var count = bucketList.list.RemoveAll(in entityData);
            this.count -= count;
            return count;

        }

        /// <summary>
        /// Add new data at the end of the list.
        /// </summary>
        /// <param name="entityData"></param>
        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void Add(in T entityData) {

            IntrusiveHashSetGeneric<T>.InitializeComponents();
            IntrusiveHashSetGeneric<T>.Initialize(ref this);

            var bucket = (entityData.GetHashCode() & 0x7fffffff) % this.buckets.Length;
            var bucketEntity = this.buckets[bucket];
            if (bucketEntity.IsAlive() == false) {
                
                bucketEntity = this.buckets[bucket] = new Entity("IntrusiveHashSetBucketGeneric<T>");
                bucketEntity.ValidateData<IntrusiveHashSetBucketGeneric<T>>();

            }
            ref var bucketList = ref bucketEntity.GetData<IntrusiveHashSetBucketGeneric<T>>();
            bucketList.list.Add(entityData);
            ++this.count;

        }
        
        /// <summary>
        /// Get an element by Equals check and hashcode
        /// </summary>
        /// <param name="hashcode"></param>
        /// <param name="element"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public bool Get(int hashcode, T element, out T output) {

            IntrusiveHashSetGeneric<T>.Initialize(ref this);
            
            var bucket = (hashcode & 0x7fffffff) % this.buckets.Length;
            var bucketEntity = this.buckets[bucket];
            if (bucketEntity.IsAlive() == true) {

                ref var bucketList = ref bucketEntity.GetData<IntrusiveHashSetBucketGeneric<T>>();
                foreach (var item in bucketList.list) {

                    if (element.Equals(item) == true) {

                        output = item;
                        return true;

                    }

                }

            }

            output = default;
            return false;

        }

        #region Helpers
        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void Initialize(ref IntrusiveHashSetGeneric<T> hashSet) {

            if (hashSet.buckets.Length == 0) hashSet.buckets = new StackArray10<Entity>(10);

        }
        
        private static void InitializeComponents() {

            WorldUtilities.InitComponentTypeId<IntrusiveHashSetBucketGeneric<T>>();
            ComponentInitializer.Init(ref Worlds.currentWorld.GetStructComponents());

        }
        
        private static class ComponentInitializer {
    
            public static void Init(ref ME.ECS.StructComponentsContainer structComponentsContainer) {
    
                structComponentsContainer.Validate<IntrusiveHashSetBucketGeneric<T>>(false);
                
            }

        }
        #endregion

    }

}
