using System.Numerics;
 using Content.Server.Shuttles.Components;
 using Robust.Server.GameObjects;
 using Content.Shared.Audio;
 using Robust.Shared.Audio;
 using Robust.Shared.Map;
 using Robust.Shared.Physics.Events;
 using Robust.Shared.Map.Components;
 using Content.Shared.Damage;

 namespace Content.Server.Shuttles.Systems;

 public sealed partial class ShuttleSystem
 {
     private const int MinimumImpactVelocity = 10;

     private readonly SoundCollectionSpecifier _shuttleImpactSound = new("ShuttleImpactSound");

     private void InitializeImpact()
     {
         SubscribeLocalEvent<ShuttleComponent, StartCollideEvent>(OnShuttleCollide);
     }

     private void OnShuttleCollide(EntityUid uid, ShuttleComponent component, ref StartCollideEvent args)
     {
         // Theta: change check from "if we're a shuttle" to "both must be grids"
         if (!TryComp<MapGridComponent>(uid, out var ourGrid) ||
             !TryComp<MapGridComponent>(args.OtherEntity, out var otherGrid))
             return;

         var ourBody = args.OurBody;
         var otherBody = args.OtherBody;

         // TODO: Would also be nice to have a continuous sound for scraping.
         var ourXform = Transform(uid);

         if (ourXform.MapUid == null)
             return;

         var otherXform = Transform(args.OtherEntity);

         var ourPoint = Vector2.Transform(args.WorldPoint, _transform.GetInvWorldMatrix(ourXform));
         var otherPoint = Vector2.Transform(args.WorldPoint, _transform.GetInvWorldMatrix(otherXform));

         var ourVelocity = _physics.GetLinearVelocity(uid, ourPoint, ourBody, ourXform);
         var otherVelocity = _physics.GetLinearVelocity(args.OtherEntity, otherPoint, otherBody, otherXform);
         var jungleDiff = (ourVelocity - otherVelocity).Length();

         if (jungleDiff < MinimumImpactVelocity)
         {
             return;
         }

         var coordinates = new EntityCoordinates(ourXform.MapUid.Value, args.WorldPoint);
         var volume = MathF.Min(10f, 1f * MathF.Pow(jungleDiff, 0.5f) - 5f);
         var audioParams = AudioParams.Default.WithVariation(SharedContentAudioSystem.DefaultVariation).WithVolume(volume);

         _audio.PlayPvs(_shuttleImpactSound, coordinates, audioParams);
     }
 }
