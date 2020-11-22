﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using homepageBackend.Data;
using homepageBackend.Domain;
using Microsoft.EntityFrameworkCore;

namespace homepageBackend.Services
{
    public class ProjectService : IProjectService
    {
        private readonly DataContext _dataContext;

        public ProjectService(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<List<Project>> GetProjectsAsync()
        {
            return await _dataContext.Projects.Include(a => a.Tags).ToListAsync();
        }

        public async Task<Project> GetProjectByIdAsync(Guid projectId)
        {
            return await _dataContext.Projects
                .Include(a => a.Tags)
                .SingleOrDefaultAsync(a => a.Id == projectId);
        }

        // creates assigned tags if they are not already in the database
        public async Task<bool> CreateProjectAsync(Project project)
        {
            project.Tags?.ForEach(x => x.TagName = x.TagName.ToLower());

            await AddNewTags(project); 
            await _dataContext.Projects.AddAsync(project);

            var created = await _dataContext.SaveChangesAsync();
            return created > 0;
        }

        // creates assigned tags if they are not already in the database
        public async Task<bool> UpdateProjectAsync(Project projectToUpdate)
        {
            projectToUpdate.Tags?.ForEach(x=>x.TagName = x.TagName.ToLower());
            await AddNewTags(projectToUpdate);
            _dataContext.Projects.Update(projectToUpdate);
            var updated = await _dataContext.SaveChangesAsync();
            return updated > 0;
        }

        public async Task<bool> DeleteProjectAsync(Guid projectId)
        {
            var project = await GetProjectByIdAsync(projectId);

            if (project == null)
                return false;

            _dataContext.Projects.Remove(project);
            var deleted = await _dataContext.SaveChangesAsync();
            return deleted > 0;
        }

        public async Task<bool> UserOwnsPostAsync(Guid projectId, string userId)
        {
            var project = await _dataContext.Projects.AsNoTracking().SingleOrDefaultAsync(a => a.Id == projectId);

            if (project == null)
            {
                return false;
            }

            if (project.UserId != userId)
            {
                return false;
            }

            return true;
        }

        public async Task<List<Tag>> GetAllTagsAsync()
        {
            return await _dataContext.Tags.AsNoTracking().ToListAsync();
        }

        public async Task<Tag> GetTagByNameAsync(string tagName)
        {
            return await _dataContext.Tags.AsNoTracking().SingleOrDefaultAsync(x => x.Name == tagName.ToLower());
        }

        public async Task<bool> CreateTagAsync(Tag tag)
        {
            tag.Name = tag.Name.ToLower();
            var existingTag = await _dataContext.Tags.AsNoTracking().SingleOrDefaultAsync(x => x.Name == tag.Name);
            if (existingTag != null)
                return true;

            await _dataContext.Tags.AddAsync(tag);
            var created = await _dataContext.SaveChangesAsync();
            return created > 0;
        }

        public async Task<bool> DeleteTagAsync(string tagName)
        {
            var tag = await _dataContext.Tags.AsNoTracking().SingleOrDefaultAsync(x => x.Name == tagName.ToLower());

            if (tag == null)
                return true;

            var postTags = await _dataContext.PostTags.Where(x => x.TagName == tagName.ToLower()).ToListAsync();

            _dataContext.PostTags.RemoveRange(postTags);
            _dataContext.Tags.Remove(tag);
            return await _dataContext.SaveChangesAsync() > postTags.Count;
        }

        private async Task AddNewTags(Project post)
        {
            foreach (var tag in post.Tags)
            {
                var existingTag =
                    await _dataContext.Tags.SingleOrDefaultAsync(x =>
                        x.Name == tag.TagName);
                if (existingTag != null)
                    continue;

                await _dataContext.Tags.AddAsync(new Tag
                    {Name = tag.TagName, CreatedOn = DateTime.UtcNow, CreatorId = post.UserId});
            }
        }
    }
}