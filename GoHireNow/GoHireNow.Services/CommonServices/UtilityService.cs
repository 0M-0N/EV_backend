using System;

namespace GoHireNow.Service.CommonServices
{
    public static class UtilityService
    {
        public static bool IsValid<T>(int value)
        {
            return Enum.IsDefined(typeof(T), value);
        }
    }
}
