using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NbuLibrary.Core.DataModel
{
    public class RelationModel : EntityModel
    {
        public RelationModel(EntityModel entity1, EntityModel entity2, RelationType type, string role)
        {
            var isLeft = entity1.Name.ToLower().CompareTo(entity2.Name.ToLower()) < 0;
            Left = isLeft ? entity1 : entity2;
            Right = isLeft ? entity2 : entity1;
            if (!isLeft)
            {
                if (type == RelationType.OneToMany)
                    type = RelationType.ManyToOne;
                else if (type == RelationType.ManyToOne)
                    type = RelationType.OneToMany;
            }

            Type = type;
            Role = role;

            Name = string.Format("{0}_{1}_{2}", Left.Name, Right.Name, Role);
        }

        public RelationModel(RelationModel model) : base(model)
        {
            Left = model.Left;
            Right = model.Right;
            Role = model.Role;
            Type = model.Type;
        }

        public EntityModel Left { get; set; }
        public EntityModel Right { get; set; }
        public string Role { get; set; }
        public RelationType Type { get; set; }

        public bool Contains(string entity)
        {
            return Left.Name == entity || Right.Name == entity;
        }

        public RelationType TypeFor(string entity)
        {
            if (!Contains(entity)) throw new ArgumentException("The entity is not part of the relation.");
            bool isLeft = Left.Name == entity;
            if (isLeft)
                return Type;
            else if (Type == RelationType.ManyToOne)
                return RelationType.OneToMany;
            else if (Type == RelationType.OneToMany)
                return RelationType.ManyToOne;
            else
                return Type;
        }
        public EntityModel GetOther(string entity)
        {
            if (entity.Equals(Left.Name, StringComparison.InvariantCultureIgnoreCase))
                return Right;
            else if (entity.Equals(Right.Name, StringComparison.InvariantCultureIgnoreCase))
                return Left;
            else
                throw new ArgumentException("The entity is not part of the relation.");
        }
    }

    public enum RelationType
    {
        OneToOne,
        OneToMany,
        ManyToOne,
        ManyToMany
    }
}
