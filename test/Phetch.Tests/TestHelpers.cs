namespace Phetch.Tests
{
    using System.Threading.Tasks;

    public class TestHelpers
    {
        /// <summary>
        /// Equivalent to <see cref="Task.FromResult{TResult}(TResult)"/>, but forces a yield so
        /// that the task doesn't complete synchronously.
        /// </summary>
        public static async Task<T> ReturnAsync<T>(T value)
        {
            await Task.Yield();
            return value;
        }
    }
}
