using System;
using System.Threading.Tasks;
using LocalRagApplication.Models;
using LocalRagApplication.Services;

namespace LocalRagApplication.Tests.TestDoubles
{
    /// <summary>
    /// <see cref="IQueryService"/> の手書きテストダブル。固定の <see cref="AnswerResult"/> を返す、
    /// または指定した例外をスローする2通りの動作を選べる。
    /// </summary>
    public class FakeQueryService : IQueryService
    {
        private readonly AnswerResult _result;
        private readonly Exception _exceptionToThrow;

        /// <summary>
        /// <see cref="AskAsync"/> が返す固定の結果を指定して初期化する。
        /// </summary>
        /// <param name="result"><see cref="AskAsync"/> の戻り値として返す回答結果。</param>
        public FakeQueryService(AnswerResult result)
        {
            _result = result;
        }

        /// <summary>
        /// <see cref="AskAsync"/> がスローする例外を指定して初期化する。
        /// </summary>
        /// <param name="exceptionToThrow"><see cref="AskAsync"/> の呼び出し時にスローする例外。</param>
        public FakeQueryService(Exception exceptionToThrow)
        {
            _exceptionToThrow = exceptionToThrow;
        }

        /// <summary>
        /// <see cref="AskAsync"/> が呼び出された回数。
        /// </summary>
        public int AskCallCount { get; private set; }

        /// <summary>
        /// 直近の <see cref="AskAsync"/> 呼び出しで渡された質問文。
        /// </summary>
        public string LastQuestion { get; private set; }

        /// <inheritdoc />
        public Task<AnswerResult> AskAsync(string question)
        {
            AskCallCount++;
            LastQuestion = question;

            if (_exceptionToThrow != null)
            {
                throw _exceptionToThrow;
            }

            return Task.FromResult(_result);
        }
    }
}
