namespace Toggl.Multivac
{
    public static class HashCode
    {
        public static int seed = 17;
        public static int hashMultiplier = 92821;
        
        public static int From<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13, T14 item14, T15 item15)
        {
            unchecked
            {
                return hashMultiplier * From(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12, item13, item14, item15) + item0.GetHashCode();
            }
        }

        public static int From<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13, T14 item14)
        {
            unchecked
            {
                return hashMultiplier * From(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12, item13, item14) + item0.GetHashCode();
            }
        }

        public static int From<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13)
        {
            unchecked
            {
                return hashMultiplier * From(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12, item13) + item0.GetHashCode();
            }
        }

        public static int From<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12)
        {
            unchecked
            {
                return hashMultiplier * From(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12) + item0.GetHashCode();
            }
        }

        public static int From<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11)
        {
            unchecked
            {
                return hashMultiplier * From(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11) + item0.GetHashCode();
            }
        }

        public static int From<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10)
        {
            unchecked
            {
                return hashMultiplier * From(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10) + item0.GetHashCode();
            }
        }

        public static int From<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9)
        {
            unchecked
            {
                return hashMultiplier * From(item1, item2, item3, item4, item5, item6, item7, item8, item9) + item0.GetHashCode();
            }
        }

        public static int From<T0, T1, T2, T3, T4, T5, T6, T7, T8>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8)
        {
            unchecked
            {
                return hashMultiplier * From(item1, item2, item3, item4, item5, item6, item7, item8) + item0.GetHashCode();
            }
        }

        public static int From<T0, T1, T2, T3, T4, T5, T6, T7>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
        {
            unchecked
            {
                return hashMultiplier * From(item1, item2, item3, item4, item5, item6, item7) + item0.GetHashCode();
            }
        }

        public static int From<T0, T1, T2, T3, T4, T5, T6>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
        {
            unchecked
            {
                return hashMultiplier * From(item1, item2, item3, item4, item5, item6) + item0.GetHashCode();
            }
        }

        public static int From<T0, T1, T2, T3, T4, T5>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
        {
            unchecked
            {
                return hashMultiplier * From(item1, item2, item3, item4, item5) + item0.GetHashCode();
            }
        }

        public static int From<T0, T1, T2, T3, T4>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4)
        {
            unchecked
            {
                return hashMultiplier * From(item1, item2, item3, item4) + item0.GetHashCode();
            }
        }

        public static int From<T0, T1, T2, T3>(T0 item0, T1 item1, T2 item2, T3 item3)
        {
            unchecked
            {
                return hashMultiplier * From(item1, item2, item3) + item0.GetHashCode();
            }
        }

        public static int From<T0, T1, T2>(T0 item0, T1 item1, T2 item2)
        {
            unchecked
            {
                return hashMultiplier * From(item1, item2) + item0.GetHashCode();
            }
        }

        public static int From<T0, T1>(T0 item0, T1 item1)
        {
            unchecked
            {
                var hash = seed;
                hash = hash * hashMultiplier + item0.GetHashCode();
                hash = hash * hashMultiplier + item1.GetHashCode();
                return hash;
            }
        }
    }
}


