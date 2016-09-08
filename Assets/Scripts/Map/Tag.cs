public class Tag : BaseNode {
	public string Key { set; get; }
	public string Value { set; get; }

	public Tag (string key, string value) {
		Key = key;
		Value = value;
	}
}
