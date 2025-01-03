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

		[Property, Title("Interaction Name")]
		public string InteractionString {get; set;} = "Interact";

		[Property]
		public float InteractionDistance {get; set;} = 120f;

		[Property]
		public bool InteractionEnabled {get; set;} = true;

		/// <summary>
		/// If not set, will try to find a collider on the same GameObject.
		/// </summary>
		[Property, Title("Override collider")]
		public Collider Collider { get; set; }

		static protected GameObject InteractionPanelPrefab ;
		private GameObject CurrentPanel = null;


		protected override void OnStart()
		{

			InteractionPanelPrefab = GameObject.GetPrefab("InteractionsPanel.prefab");

			if (!InteractionPanelPrefab.IsValid())
			{
				Log.Error($"No InteractionPanel prefab found for {this.GameObject.Name}!");
			}

			if (!Collider.IsValid()) {
				
				Collider = this.GameObject.GetComponent<Collider>();

				if (!Collider.IsValid())
				{
					Log.Error($"No collider found for {this.GameObject.Name}!");
				}
			}
		}

		protected override async void OnUpdate()
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
			Rotation camRotation = Scene.Camera.WorldRotation;

			Angles ang = camRotation.Angles();
			ang.roll += 180;
			ang.pitch += 180;
			Rotation rot = ang.ToRotation();
			CurrentPanel.WorldRotation = rot;

			InteractionPanel panel = CurrentPanel.GetComponent<InteractionPanel>();
			panel.InteractionString = InteractionString;

			if (Input.Pressed("use"))
			{
				_ = panel.TriggerInteractAnimation();
				OnInteract();
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