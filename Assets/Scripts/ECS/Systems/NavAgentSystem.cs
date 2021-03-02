using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using TWD_Components;

namespace TWD_Systems
{
    [UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
    public class NavAgentSystem : JobComponentSystem
    {
        EntityCommandBufferSystem setDestinationEntityCommandBuffer;
        EntityCommandBufferSystem pathSuccessEntityCommandBuffer;
        EntityCommandBufferSystem pathErrorEntityCommandBuffer;

        protected static NavAgentSystem instance;

        private EntityQuery agentsDataQuery;
        private NativeQueue<AgentData> needsWaypoint = new NativeQueue<AgentData>(Allocator.Persistent);

        private struct AgentData
        {
            public Entity entity;
            public NavAgent agent;
        }

        private ConcurrentDictionary<int, Vector3[]> waypointsDictionary = new ConcurrentDictionary<int, Vector3[]>();
        private NativeHashMap<int, AgentData> pathFindingData;

        private NavMeshQuerySystem querySystem;

        protected override void OnCreate()
        {
            instance = this;
            agentsDataQuery = GetEntityQuery(typeof(Zombie), typeof(NavAgent));
            setDestinationEntityCommandBuffer = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            pathSuccessEntityCommandBuffer = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EntityCommandBufferSystem>();
            pathErrorEntityCommandBuffer = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EntityCommandBufferSystem>();
            querySystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<NavMeshQuerySystem>();
            querySystem.RegisterPathResolvedCallback(OnPathSuccess);
            querySystem.RegisterPathFailedCallback(OnPathError);
            pathFindingData = new NativeHashMap<int, AgentData>(0, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            pathFindingData.Dispose();
            needsWaypoint.Dispose();
        }

        //private struct SetNextWaypointJob : IJob
        //{
        //    public NativeQueue<AgentData> needsWaypoint;
        //    public NativeArray<NavAgent> agents;
        //    public void Execute()
        //    {
        //        while (needsWaypoint.TryDequeue(out AgentData item))
        //        {
        //            if (NavAgentSystem.instance.waypointsDictionary.TryGetValue(item.entity.Index, out Vector3[] currentWaypoints))
        //            {
        //                agent.currentWaypoint = currentWaypoints[agent.nextWaypointIndex];
        //                agent.remainingDistance = Vector3.Distance(agent.position, agent.currentWaypoint);
        //                agent.nextWaypointIndex++;
        //            }
        //            agent.needsWaypoint = false;
        //        }
        //    }
        //}

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var dt = Time.DeltaTime;
            var navMeshQuerySystemVersion = querySystem.Version;
            var needsWaypointParallelWriter = needsWaypoint.AsParallelWriter();
            var waypoints = waypointsDictionary;
            inputDeps = Entities
                .WithAll<Zombie>()
                .WithName("DetectNextWaypointJob")
                .ForEach((ref NavAgent agent,
                ref WaitingEntity waitingEntity,
                in Entity entity) =>
                {
                    if (agent.remainingDistance < agent.stoppingDistance && agent.status == AgentStatus.Moving)
                    {
                        if (agent.nextWaypointIndex != agent.totalWaypoints)
                        {
                            agent.needsWaypoint = true;
                            needsWaypointParallelWriter.Enqueue(new AgentData { entity = entity, agent = agent });
                        }
                        else if (navMeshQuerySystemVersion != agent.queryVersion || agent.nextWaypointIndex == agent.totalWaypoints)
                        {
                            agent.totalWaypoints = 0;
                            agent.currentWaypoint = 0;
                            agent.nextWaypointIndex = 0;
                            agent.status = AgentStatus.Idle;
                            if (!waitingEntity.isWaiting)
                            {
                                waitingEntity.ready = false;
                            }
                        }
                    }
                }).Schedule(inputDeps);
            inputDeps.Complete();

            Entities
                .WithoutBurst()
                .WithName("SetNextWaypointJob")
                .WithAll<Zombie>()
                .WithReadOnly(waypoints)
                .ForEach((ref NavAgent agent,
                in Entity entity) =>
                {
                    if (agent.needsWaypoint)
                    {
                        if (waypoints.TryGetValue(entity.Index, out Vector3[] currentWaypoints))
                        {
                            agent.currentWaypoint = currentWaypoints[agent.nextWaypointIndex];
                            agent.remainingDistance = Vector3.Distance(agent.position, agent.currentWaypoint);
                            agent.nextWaypointIndex++;
                        }
                        agent.needsWaypoint = false;
                    }
                }).Run();

            //inputDeps = new SetNextWaypointJob
            //{
            //    needsWaypoint = needsWaypoint,
            //    agents = agentsDataQuery.ToComponentDataArray<NavAgent>(Allocator.TempJob)
            //}.Schedule(inputDeps);

            inputDeps = Entities
                .WithAll<Zombie>()
                .WithName("MovementJob")
                .ForEach((ref NavAgent agent) =>
                {
                    if (agent.remainingDistance > agent.stoppingDistance && agent.status == AgentStatus.Moving)
                    {
                        agent.currentMoveSpeed = Mathf.Lerp(agent.currentMoveSpeed, agent.moveSpeed, dt * agent.acceleration);

                        if (agent.nextPosition.x != Mathf.Infinity)
                        {
                            agent.position = agent.nextPosition;
                        }

                        var heading = (Vector3)(agent.currentWaypoint - agent.position);
                        agent.remainingDistance = heading.magnitude;
                        if (agent.remainingDistance > 0.001f)
                        {
                            var targetRotation = Quaternion.LookRotation(heading, Vector3.up).eulerAngles;
                            targetRotation.x = targetRotation.z = 0;
                            if (agent.remainingDistance < 1)
                            {
                                agent.rotation = Quaternion.Euler(targetRotation);
                            }
                            else
                            {
                                agent.rotation = Quaternion.Slerp(agent.rotation, Quaternion.Euler(targetRotation), dt * agent.rotationSpeed);
                            }
                        }
                        var forward = math.forward(agent.rotation) * agent.currentMoveSpeed * dt;
                        agent.nextPosition = agent.position + forward;

                        if (agent.nextPosition.x == Mathf.Infinity)
                        {
                            agent.status = AgentStatus.Idle;

                        }
                    }
                    else if (agent.nextWaypointIndex == agent.totalWaypoints)
                    {
                        if (agent.currentMoveSpeed > 0)
                        {
                            agent.currentMoveSpeed = 0;
                        }
                        agent.status = AgentStatus.Idle;
                    }
                }).Schedule(inputDeps);
            return inputDeps;
        }
        public void SetDestination(Entity entity, NavAgent agent, Vector3 destination, int areas = -1)
        {
            if (pathFindingData.TryAdd(entity.Index, new AgentData { entity = entity, agent = agent }))
            {
                var command = setDestinationEntityCommandBuffer.CreateCommandBuffer();
                agent.status = AgentStatus.PathQueued;
                agent.destination = destination;
                agent.queryVersion = querySystem.Version;
                command.SetComponent<NavAgent>(entity, agent);
                querySystem.RequestPath(entity.Index, agent.position, agent.destination, areas);
            }
        }

        public static void SetDestinationStatic(Entity entity, NavAgent agent, Vector3 destination, int areas = -1)
        {
            instance.SetDestination(entity, agent, destination, areas);
        }

        private void SetWaypoint(Entity entity, NavAgent agent, Vector3[] newWaypoints)
        {
            waypointsDictionary[entity.Index] = newWaypoints;
            var command = pathSuccessEntityCommandBuffer.CreateCommandBuffer();
            agent.status = AgentStatus.Moving;
            agent.nextWaypointIndex = 1;
            agent.totalWaypoints = newWaypoints.Length;
            agent.currentWaypoint = newWaypoints[0];
            agent.remainingDistance = Vector3.Distance(agent.position, agent.currentWaypoint);
            command.SetComponent<NavAgent>(entity, agent);
        }

        private void OnPathSuccess(int index, Vector3[] waypoints)
        {
            if (pathFindingData.TryGetValue(index, out AgentData entry))
            {
                pathFindingData.Remove(index);
                SetWaypoint(entry.entity, entry.agent, waypoints);
            }
        }

        private void OnPathError(int index, PathfindingFailedReason reason)
        {
            if (pathFindingData.TryGetValue(index, out AgentData entry))
            {
                pathFindingData.Remove(index);
                entry.agent.status = AgentStatus.Idle;
                var command = pathErrorEntityCommandBuffer.CreateCommandBuffer();
                command.SetComponent<NavAgent>(entry.entity, entry.agent);
            }
        }
    }
}