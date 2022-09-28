/*
 * Seedwork（适用于域模型的可重用基类和接口）
 * 这是许多开发者在项目之间共享的复制和粘贴重用类型，不是正式框架。 seedwork 可存在于任何层或库中。 但是，如果类和接口的集足够大，可能需要创建单个类库。
 */

namespace OW.DDD
{
    public interface ICommand<out T>
    {
        
    }

    public interface ICommandResult<out T>
    {
        /// <summary>
        /// 错误码。
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// 错误信息。
        /// </summary>
        public string DebugMessage { get; set; }

    }

    public interface ICommandHandler<in TRequest, out TResponse> where TRequest : ICommand<TRequest>
    {
        public TResponse Handle(TRequest datas);
    }

}
