import pandas as pd
import numpy as np
from sentence_transformers import SentenceTransformer, util
from sklearn.metrics.pairwise import cosine_similarity
from sklearn.cluster import DBSCAN, AgglomerativeClustering


def main():
    df = pd.read_csv('book.tsv', sep='\t', header=None)
    source = df.iloc[:, 3]
    source = source.dropna()
    authors = source[145:165]
    authors = pd.concat([authors, source[420:440]]) 

    model = SentenceTransformer('paraphrase-MiniLM-L6-v2')
    author_embeddings = [model.encode(author) for author in authors]

    similarity_matrix = cosine_similarity(author_embeddings, author_embeddings)

    print(similarity_matrix)

    similar_authors_groups = []
    for i, embedding in enumerate(author_embeddings):
        # Find indices of similar authors based on cosine similarity
        similar_indices = np.where(similarity_matrix[i] > 0.83)[0]
        
        # Create a group of similar authors
        similar_authors_group = [i]  # Include the current author
        similar_authors_group.extend(similar_indices)
        
        # Remove duplicates and sort the indices
        similar_authors_group = sorted(set(similar_authors_group))
        
        # Append the group to the list of similar authors groups
        if similar_authors_group not in similar_authors_groups:
            similar_authors_groups.append(similar_authors_group)

    # print(authors)
    # print(author_embeddings)
    print(similar_authors_groups)

    name_groups = []
    for group in similar_authors_groups:
        name_group = [authors.iloc[i] for i in group]
        name_groups.append(name_group)

    print()
    print(name_groups)

    print('\n#######################\n')

    X = np.array(author_embeddings)
    
    # Initialize DBSCAN
    dbscan = DBSCAN(eps=0.5, min_samples=5)
    
    # Perform clustering
    labels = dbscan.fit_predict(X)

    print(labels)
    print(len(labels))
    print(authors)
    print(len(authors))

    print('\n#######################\n')

    # Compute pairwise cosine similarity
    cosine_similarities = cosine_similarity(author_embeddings)

    # Perform hierarchical clustering
    # You may need to adjust parameters such as linkage and distance_threshold based on your data
    clustering = AgglomerativeClustering(n_clusters=None, linkage='average', distance_threshold=0.8).fit(cosine_similarities)

    # Get cluster labels
    cluster_labels = clustering.labels_

    # Group authors based on cluster labels
    clustered_authors = {}
    for i, label in enumerate(cluster_labels):
        if label not in clustered_authors:
            clustered_authors[label] = []
        clustered_authors[label].append(authors.iloc[i])

    # Print authors in each cluster
    for cluster, authors in clustered_authors.items():
        print(f'Cluster {cluster}:')
        print(authors)





if __name__ == "__main__":
    main()