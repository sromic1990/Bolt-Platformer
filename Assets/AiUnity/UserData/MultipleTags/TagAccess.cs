using AiUnity.Common.Attributes;
using System.Collections.Generic;
using AiUnity.Common.Tags;

/// <Summary>
/// Provide strongly typed access to Unity tags.
/// <Summary>
[GeneratedType("04/05/2019 16:26:20")]
public class TagAccess : ITagAccess
{
	public const string Untagged = "Untagged";
	public const string Respawn = "Respawn";
	public const string Finish = "Finish";
	public const string EditorOnly = "EditorOnly";
	public const string MainCamera = "MainCamera";
	public const string Player = "Player";
	public const string GameController = "GameController";
	public const string Platform = "Platform";
	public const string Projectile = "Projectile";
	public const string Key = "Key";
	public const string Enemy = "Enemy";

	private static readonly List<string> tagPaths = new List<string>()
	{
		"Untagged",
		"Respawn",
		"Finish",
		"EditorOnly",
		"MainCamera",
		"Player",
		"GameController",
		"Platform",
		"Projectile",
		"Key",
		"Enemy"
	};

	public IEnumerable<string> TagPaths { get { return tagPaths.AsReadOnly(); } }

}

