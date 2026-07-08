namespace MonkeyIslandSpecialEditionSpriteEditor.Lib
{
	public readonly struct Result<T, E>
	{
		public bool IsSuccess { get; }
		public T Value { get; }
		public E Error { get; }

		private Result(T value)
		{
			this.IsSuccess = true;
			this.Value = value;
			this.Error = default!;
		}

		private Result(E error)
		{
			this.IsSuccess = false;
			this.Value = default!;
			this.Error = error;
		}
    
		public static Result<T, E> Success(T value) => new Result<T, E>(value);
		public static Result<T, E> Fail(E error) => new Result<T, E>(error);
	}
}