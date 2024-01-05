using Sandbox;
using System;
using System.Linq;

public sealed class PlayerManager : Component
{
	[Property] CapsuleCollider playerCollider { get; set; }
	[Property] GameObject BackupSpawn { get; set; }
	[Property] SoundEvent DeathSound { get; set; }
	[Property] float DeathVolume { get; set; } = 10f;
	[Property] SoundEvent CheckpointSound { get; set; }
	[Property] float CheckpointVolume { get; set; } = 10f;

	GameObject currentCheckpoint;
	protected override void OnFixedUpdate()
	{
		foreach (Collider collider in playerCollider.Touching)
		{
			if (collider.Tags.Has("kill"))
			{
				Kill();
				break;
			}
			else if (collider.Tags.Has("checkpoint") && collider.GameObject.Components.Get<ModelRenderer>().MaterialOverride != Cloud.Material("https://asset.party/fishum/dev_neongreen"))
			{
				currentCheckpoint = collider.GameObject;
				collider.GameObject.Components.Get<ModelRenderer>().MaterialOverride = Cloud.Material("https://asset.party/fishum/dev_neongreen");
				SoundHandle soundCheckpoint = Sound.Play(CheckpointSound, Transform.Position);
				soundCheckpoint.Volume = CheckpointVolume;
				break;
			}
		}
		void Kill()
		{
			if(currentCheckpoint == null){
				Transform.Position = BackupSpawn.Transform.Position;
			} else{
				Transform.Position = currentCheckpoint.Transform.Position;
			}
			SoundHandle soundKilled = Sound.Play(DeathSound, Transform.Position);
			soundKilled.Volume = DeathVolume;
		}
	}
}
