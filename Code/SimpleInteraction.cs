using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.UI;

namespace SimpleInteractions {

	/// <summary>
	/// Simple interaction component
	/// </summary>
	[Title( "Simple Interaction" )]
	public class SimpleInteraction : Component
	{
		[Property]
		public bool InteractionEnabled {get; set;} = true;

		[Property, Title("Interaction Name")]
		public string InteractionString {get; set;} = "Interact";

		[Property]
		public float InteractionDistance {get; set;} = 120f;

		[Property, ToggleGroup("InteractionHold")]
		public bool InteractionHold {get; set;} = false;

		[Property, Group("InteractionHold")]
		public float InteractionHoldDuration {get; set;} = 1f;


		/// <summary>
		/// If not set, will try to find a collider on the same GameObject.
		/// </summary>
		[Property, Title("Override collider")]
		public Collider Collider { get; set; }

		static protected GameObject InteractionPanelPrefab ;
		private GameObject CurrentPanel = null;
		private TimeSince HoldTime = 0;
		private bool Holding = false;
		private bool HoldingInteractionHappened = false;


		protected override void OnStart()
		{

			InteractionPanelPrefab = GameObject.GetPrefab("InteractionsPanel.prefab");

			Assert.True(InteractionPanelPrefab.IsValid(), $"No InteractionPanel prefab found for {this.GameObject.Name}!");

			if (!Collider.IsValid()) {
				
				Collider = this.GameObject.GetComponent<Collider>();

				Assert.True(Collider.IsValid(), $"No collider found for {this.GameObject.Name}!");
			}
		}

		protected override void OnUpdate()
		{

			if (!InteractionEnabled) return;

			Ray ray = Scene.Camera.GameObject.Transform.World.ForwardRay;

			SceneTraceResult tr = Scene.Trace.Ray(ray, InteractionDistance)
			.WithoutTags("player")
			.Run();

			
			// Gizmo.Draw.Line(tr.StartPosition, tr.EndPosition);
			if (tr.Hit)
			{
				Collider hitCollider = tr.GameObject.GetComponent<Collider>();

				if (hitCollider == Collider)
				{
					OnHover(tr);
				} else
				{
					_ = DeletePanel();
				}
			} else
			{
				_ = DeletePanel();
			}
		}

		private void OnHover(SceneTraceResult tr)
		{

			if (!CurrentPanel.IsValid())
			{
				CurrentPanel = InteractionPanelPrefab.Clone();
			}

			Vector3 pos = tr.GameObject.WorldPosition;
			CurrentPanel.WorldPosition = pos;

			// Flip the panel to face the camera
			Rotation camRotation = Scene.Camera.WorldRotation;

			Angles ang = camRotation.Angles();
			ang.roll += 180;
			ang.pitch += 180;
			Rotation rot = ang.ToRotation();
			CurrentPanel.WorldRotation = rot;

			InteractionPanel panel = CurrentPanel.GetComponent<InteractionPanel>();
			panel.InteractionString = InteractionString;



			if (!InteractionHold)
			{
				if (Input.Pressed("use"))
				{
					_ = panel.TriggerInteractAnimation();
					OnInteract();
				}
				return;
			}

			
			if (!Input.Down("use"))
			{
				Holding = false;
				HoldingInteractionHappened = false;
				return;
			}

			// Interaction already happened. Player needs to release and press again.
			if (HoldingInteractionHappened)
			{
				return;
			}

			if (Holding)
			{
				if (HoldTime.Relative >= InteractionHoldDuration)
				{
					HoldingInteractionHappened = true;
					OnInteract();
				}
			} else
			{
				// Started holding.
				Holding = true;
				HoldTime = 0;
			}
		}

		async private Task DeletePanel()
		{
			if(!CurrentPanel.IsValid()) return;

			CurrentPanel.GetComponent<PanelComponent>().Panel.Delete();
			await Task.DelaySeconds( 0.1f );
			CurrentPanel.Destroy();
		}

		[Rpc.Owner]
		protected virtual void OnInteract()
		{
			Log.Error($"Interaction not implemented for {this.GameObject.Name}!");
		}

	}

}